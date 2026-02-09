using Realms;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SyncDemo.WpfApp;

internal static class Extensions
{
  #region --- Detach ---------------------------------------------------------

  private static readonly Type RealmObjectInterface = typeof(IRealmObject);

  /// <summary>
  /// Erzeugt eine Vollständige Graph-Kopie von dem RealmObject, inkl. aller verschachtelten Objekte und Listen.
  /// ReadOnly-Collection ohne Setter werden übersprungen
  /// </summary>
  /// <typeparam name="T"></typeparam>
  /// <param name="obj"></param>
  /// <param name="maxDepth">Die maximale Tiefe der verschachtelten Object die Kopiert werden sollen</param>
  /// <returns></returns>
  public static T? Detach<T>(this T? obj, int maxDepth = 3)
    where T : class, IRealmObject
    => (T?)detachInternal(obj, 0, maxDepth, new Dictionary<IRealmObject, IRealmObject>());

  private static object? detachInternal(object? value, int depth, int maxDepth, IDictionary<IRealmObject, IRealmObject> cache)
  {
    if (value == null)
      return null;

    //Wenn IsValid false dann das Object zurückgeben, da es nicht mehr zu einem Realm gehört und detached werden muss
    if (value is IRealmObject o && (!o.IsValid || o.Realm == null))
      return value;

    if (depth > maxDepth)
      return null;

    // Primitive / immutable types durchreichen
    if (value is string || value.GetType().IsPrimitive || value is decimal || value is DateTime || value is Guid || value is DateTimeOffset || value is Enum)
      return value;

    // Listen
    if (value is IEnumerable enumerable && value is not IRealmObject && value is not byte[])
    {
      var elementType = getEnumerableElementType(value.GetType()) ?? typeof(object);
      var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType))!;
      foreach (var item in enumerable)
        list.Add(detachInternal(item, depth + 1, maxDepth, cache));
      return list;
    }

    // RealmObject?
    if (value is IRealmObject realmObj)
    {
      if (cache.TryGetValue(realmObj, out var existing))
        return existing;

      var type = value.GetType();
      var clone = (IRealmObject)Activator.CreateInstance(type)!;
      cache[realmObj] = clone;

      foreach (var prop in getCopyableProperties(type))
      {
        var propType = prop.PropertyType;
        var originalVal = prop.GetValue(value);

        if (originalVal == null)
        {
          if (prop.CanWrite)
            prop.SetValue(clone, null);
          continue;
        }

        // Collections (nur wenn setzbar)
        if (typeof(IEnumerable).IsAssignableFrom(propType) && propType != typeof(string))
        {
          if (!prop.CanWrite)
            continue; // Skip readonly Realm-Collection (könnte alternativ gefüllt werden, falls Instanz existiert)

          var detachedCol = detachInternal(originalVal, depth + 1, maxDepth, cache);
          prop.SetValue(clone, detachedCol);
          continue;
        }

        // Verschachtelte RealmObject / komplex
        if (RealmObjectInterface.IsAssignableFrom(propType))
        {
          var detachedChild = detachInternal(originalVal, depth + 1, maxDepth, cache);
          if (prop.CanWrite)
            prop.SetValue(clone, detachedChild);
          continue;
        }

        // Primitive / direkte Kopie
        if (prop.CanWrite)
          prop.SetValue(clone, originalVal);
      }

      return clone;
    }

    // Fallback: tiefe Kopie nicht nötig
    return value;
  }

  private static IEnumerable<PropertyInfo> getCopyableProperties(Type t) =>
      t.GetProperties(BindingFlags.Instance | BindingFlags.Public)
       .Where(p =>
           p.GetIndexParameters().Length == 0 &&
           p.GetMethod != null &&
           !p.GetMethod.IsStatic &&
           p.Name is not "Realm" and not "IsManaged");

  private static Type? getEnumerableElementType(Type seqType)
  {
    if (seqType.IsArray) return seqType.GetElementType();
    var ienum = seqType.GetInterfaces()
        .Append(seqType)
        .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
    return ienum?.GetGenericArguments()[0];
  }

  #endregion

}
