using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace System
{
    public class SerializableAttribute : Attribute
    {
    }

    public abstract class SerializationInfo
    {
        public abstract object GetValue(string name, Type objectType);

        public abstract void AddValue(string name, object value, Type objectType);
    }

    public interface ISerializable
    {
        
    }
}
