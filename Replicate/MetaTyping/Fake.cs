using Replicate.MetaData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.MetaTyping
{
    public class Fake
    {
        static CustomAttributeBuilder attrbuilder = new CustomAttributeBuilder(
                typeof(ReplicateAttribute).GetConstructor(new Type[0]), new object[0]);
        private TypeBuilder builder;
        public Fake(string name)
        {
            builder = DynamicModule.Single.DefineType(name, TypeAttributes.Public);
        }
        public Fake AddField(Type type, string name)
        {
            var field = builder.DefineField(name, type, FieldAttributes.Public);
            field.SetCustomAttribute(attrbuilder);
            return this;
        }
        public Type Build() => builder.CreateType();
        public static Type FromType(Type valueType, ReplicationModel model = null)
        {
            model = model ?? ReplicationModel.Default;
            var typeName = valueType.FullName.Replace('.', '_').Replace('+', '_') + "_Fake";
            var existingType = DynamicModule.Single.GetType(typeName);
            if (existingType != null) return existingType;
            var fake = new Fake(typeName);
            var args = valueType.GetGenericArguments();
            GenericTypeParameterBuilder[] parameters = { };
            if (args.Any())
                parameters = fake.builder.DefineGenericParameters(args.Select(a => a.Name).ToArray());
            var typeData = model.GetTypeData(valueType);
            foreach (var field in typeData.ReplicatedMembers)
            {
                var memberType = field.IsGenericParameter ? parameters[field.GenericParameterPosition] : field.MemberType;
                fake.AddField(memberType, field.Name);
            }
            return fake.Build();
        }
    }
}
