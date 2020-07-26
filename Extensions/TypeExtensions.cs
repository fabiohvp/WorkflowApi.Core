using System.Collections;
using System.Linq;

namespace System
{
    public static class TypeExtensions
    {
        public static bool IsQueryable(this Type type)
        {
            return typeof(IQueryable).IsAssignableFrom(type);
        }
    }
}
