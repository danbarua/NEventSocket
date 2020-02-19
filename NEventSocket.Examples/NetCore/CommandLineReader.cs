using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using Autofac;
using Net.Reflection;
using Net.System;
using Net.Text;

namespace NEventSocket.Examples.NetCore
{
    public class CommandLineReader
    {
        private readonly ILifetimeScope _activator;

        public CommandLineReader(ILifetimeScope scope)
        {
            _activator = scope;
        }

        static readonly Type[] primitiveTypes =
        {
            typeof(string), typeof(int), typeof(int?), typeof(short), typeof(short?), typeof(long), typeof(long?), typeof(bool),
            typeof(bool?), typeof(double), typeof(double?), typeof(decimal), typeof(decimal?), typeof(DateTime), typeof(DateTime?),
            typeof(Guid), typeof(Guid?)
        };

        public T ReadObject<T>(CancellationToken cancellationToken)
        {
            return (T)ReadObject(typeof(T), cancellationToken);
        }

        public object ReadObject(Type type, CancellationToken cancellationToken, string description = "")
        {
            // TODO: add support for enums, what to do with array types?
            // handle primitive types directly
            return primitiveTypes.Contains(type)
                ? ReadPrimitiveValue(type, description, cancellationToken)
                : ReadComplexValue(type, description, cancellationToken);
        }

        private object ReadComplexValue(Type type, string title, CancellationToken cancellationToken)
        {
            var propertyInfos = type.GetProperties();
            object instance;
            try
            {
                instance = _activator.Resolve(type);
            }
            catch (Exception e)
            {
                throw new Exception("unable to instantiate type: " + type.FullName, e);
            }

            if (propertyInfos.Length > 0)
                Console.WriteLine("Specify values for {0} [{1}]", title, type.Name);

            foreach (var propertyInfo in propertyInfos)
            {
                var defaultExpression = propertyInfo.GetValue(instance).To<string>().Wrap(" [default: ", "]");
                var propertyName = propertyInfo.GetAttribute<DisplayNameAttribute>().Get(d => d.DisplayName, propertyInfo.Name)
                                   + defaultExpression;

                var value = ReadObject(propertyInfo.PropertyType, cancellationToken, propertyName);

                if (value == null)
                    continue; // keep default value

                propertyInfo.SetValue(instance, value);
            }

            return instance;
        }

        private static object ReadPrimitiveValue(Type propertyType, string propertyDescription, CancellationToken cancellationToken)
        {
            do
            {
                try
                {
                    Console.Write("Enter a [{1}] for {0}:", propertyDescription, propertyType);

                    var line = Console.ReadLine();
                    if (line.HasNoValue())
                        return null;

                    return line.To(propertyType);
                }
                catch (FormatException e)
                {
                    Console.WriteLine(e.Message);
                }
            }
            while (!cancellationToken.IsCancellationRequested);

            return null;
        }
    }
}
