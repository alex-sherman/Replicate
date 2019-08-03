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
        private GenericTypeParameterBuilder[] genericParameters;
        public Fake(string name, ModuleBuilder module, Type sourceType = null)
        {
            builder = module.DefineType(name, TypeAttributes.Public);
            if (sourceType != null) builder.SetCustomAttribute(new CustomAttributeBuilder(
                 typeof(FakeTypeAttribute).GetConstructor(new Type[0]), new object[0],
                 new[] { typeof(FakeTypeAttribute).GetField("Source") }, new object[] { sourceType }));
        }
        public GenericTypeParameterBuilder[] MakeGeneric(params string[] names)
        {
            return genericParameters = builder.DefineGenericParameters(names);
        }
        public Fake AddField(int genericPosition, string name)
        {
            return AddField(genericParameters[genericPosition], name);
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
            var existingType = model.Builder.GetType(typeName);
            if (existingType != null) return existingType;
            var fake = new Fake(typeName, model.Builder, sourceType);
            var args = sourceType.GetGenericArguments();
            if (args.Any())
                fake.MakeGeneric(args.Select(a => a.Name).ToArray());
            var typeData = model.GetTypeData(sourceType);
            foreach (var field in typeData.Members)
            {
                if (field.IsGenericParameter)
                    fake.AddField(field.GenericParameterPosition, field.Name);
                else
                    fake.AddField(field.MemberType, field.Name);
            }
            return fake.Build();
        }
    }
}
