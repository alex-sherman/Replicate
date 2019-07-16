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
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Enum)]
    public class FakeTypeAttribute : Attribute
    {
        public Type Source;
    }
    public class Fake
    {
        static CustomAttributeBuilder replicateAttrBuilder = new CustomAttributeBuilder(
                typeof(ReplicateAttribute).GetConstructor(new Type[0]), new object[0]);
        private TypeBuilder builder;
        public Fake(string name, Type sourceType = null)
        {
            builder = DynamicModule.Single.DefineType(name, TypeAttributes.Public);
            if (sourceType != null) builder.SetCustomAttribute(new CustomAttributeBuilder(
                 typeof(FakeTypeAttribute).GetConstructor(new Type[0]), new object[0],
                 new[] { typeof(FakeTypeAttribute).GetField("Source") }, new object[] { sourceType }));
        }
        public Fake AddField(Type type, string name)
        {
            var field = builder.DefineField(name, type, FieldAttributes.Public);
            field.SetCustomAttribute(replicateAttrBuilder);
            return this;
        }
        public Type Build() => builder.CreateType();
        public static Type FromType(Type sourceType, ReplicationModel model = null)
        {
            model = model ?? ReplicationModel.Default;
            var typeName = sourceType.FullName.Replace('.', '_').Replace('+', '_') + "_Fake";
            var existingType = DynamicModule.Single.GetType(typeName);
            if (existingType != null) return existingType;
            var fake = new Fake(typeName, sourceType);
            var args = sourceType.GetGenericArguments();
            GenericTypeParameterBuilder[] parameters = { };
            if (args.Any())
                parameters = fake.builder.DefineGenericParameters(args.Select(a => a.Name).ToArray());
            var typeData = model.GetTypeData(sourceType);
            foreach (var field in typeData.ReplicatedMembers)
            {
                var memberType = field.IsGenericParameter ? parameters[field.GenericParameterPosition] : field.MemberType;
                fake.AddField(memberType, field.Name);
            }
            return fake.Build();
        }
    }
}
