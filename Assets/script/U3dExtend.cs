using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    public static class U3DExtend
    {
        public static Component TryGetOrAddComponent(this GameObject self, Type type )
        {
            if (type.IsSubclassOf(typeof(Component)))
            {
                Component c = self.GetComponent(type);
                if (c == null)
                {
                    return self.AddComponent(type);
                }
                else
                {
                    return c;
                }
            }
            else
            {
                return null;
            }
        }

        public static T FetchComponent<T>(this GameObject self)
            where T:Component
        {
            return (T)self.TryGetOrAddComponent(typeof(T));
        }

        
    }
}
