using System.Linq.Expressions;
using System.Reflection;

namespace W1E1.Core
{
    public static class ColumnResolver
    {
        internal static string Resolve<T, TProp>(Expression<Func<T, TProp>> expr)
        {
            MemberExpression? member = null;
            if (expr.Body is MemberExpression m1) member = m1;
            if (expr.Body is UnaryExpression u && u.Operand is MemberExpression m2) member = m2;

            if (member is null) throw new ArgumentException($"Expression inválida para resolução de coluna: {expr}");

            string leafName = member.Member.Name;
            ValidateMember(typeof(T), member);
            return leafName;
        }

        private static void ValidateMember(Type root, MemberExpression expr)
        {
            if (expr.Expression is MemberExpression parent) ValidateMember(root, parent);

            Type ownerType = root;
            if (expr.Expression is MemberExpression m)
            {
                PropertyInfo? prop = m.Member as PropertyInfo;
                if (prop != null) ownerType = prop.PropertyType;

                FieldInfo? field = m.Member as FieldInfo;
                if (field != null) ownerType = field.FieldType;
            }

            PropertyInfo[] props = ownerType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < props.Length; i++)
            {
                if (string.Equals(props[i].Name, expr.Member.Name, StringComparison.OrdinalIgnoreCase)) return;
            }

            throw new ArgumentException($"Propriedade '{expr.Member.Name}' não encontrada em '{ownerType.Name}'.");
        }
    }
}
