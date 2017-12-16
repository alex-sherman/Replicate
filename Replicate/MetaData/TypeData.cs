﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.MetaData
{
    public class TypeData
    {
        public ReplicationPolicy Policy;
        public string Name { get; private set; }
        public Type Type { get; private set; }
        public List<MemberInfo> ReplicatedMembers = new List<MemberInfo>();
        public TypeAccessor Surrogate { get; private set; }
        public ReplicationModel Model { get; private set; }
        private bool isSurrogate = false;
        public TypeData(Type type, ReplicationModel model)
        {
            Type = type;
            Name = type.FullName;
            Model = model;
            if (type.IsPrimitive || type == typeof(string))
                Policy.MarshalMethod = MarshalMethod.Primitive;
            else
            {
                if (!type.IsValueType)
                    Policy.AllowReference = true;
                if (type.GetInterface("ICollection`1") != null)
                    Policy.MarshalMethod = MarshalMethod.Collection;
                else
                    Policy.MarshalMethod = MarshalMethod.Object;
            }

            ReplicateAttribute replicateAttribute = type.GetCustomAttribute<ReplicateAttribute>();
            if (replicateAttribute != null && replicateAttribute.MarshalMethod.HasValue)
                Policy.MarshalMethod = replicateAttribute.MarshalMethod.Value;

            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                if (field.GetCustomAttributes().Where(attr => attr is ReplicateAttribute).Any())
                    AddMember(field);
            }
            foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (property.GetCustomAttributes().Where(attr => attr is ReplicateAttribute).Any())
                    AddMember(property);
            }
        }

        public TypeData AddMember(string name)
        {
            FieldInfo fieldInfo;
            PropertyInfo propInfo;
            if ((fieldInfo = Type.GetField(name)) != null)
                AddMember(fieldInfo);
            else if ((propInfo = Type.GetProperty(name)) != null)
                AddMember(propInfo);
            else
                throw new KeyNotFoundException(string.Format("Could not find member {0} in type {1}", name, Type.Name));
            return this;
        }
        private void AddMember(FieldInfo field)
        {
            ReplicatedMembers.Add(new MemberInfo(field, (byte)ReplicatedMembers.Count));
        }
        private void AddMember(PropertyInfo property)
        {
            ReplicatedMembers.Add(new MemberInfo(property, (byte)ReplicatedMembers.Count));
        }
        public TypeData SetSurrogate(Type surrogate)
        {
            if (isSurrogate)
                throw new InvalidOperationException("Cannot set the surrogate of a surrogate type");
            if (surrogate.IsGenericTypeDefinition)
                throw new InvalidOperationException("Cannot set a surrogate type that is generic");
            var _surrogate = Model.GetTypeData(surrogate);
            _surrogate.isSurrogate = true;
            Surrogate = Model.GetTypeAccessor(surrogate);
            return _surrogate;
        }
    }
}
