using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TypedAutobahn.CodeGenerator
{
    internal class ContractMapper
    {
        private readonly IContractNameProvider mNameProvider;

        public ContractMapper(IContractNameProvider nameProvider)
        {
            mNameProvider = nameProvider;
        }

        public string MapCompositeType(Type type)
        {
            return mNameProvider.ProvideName(type);
        }

        public FunctionMetadata MapMethod(MethodInfo method)
        {
            IEnumerable<ParameterMetadata> parameters = method.GetParameters().Select
                (x => new ParameterMetadata()
                {
                    Alias = mNameProvider.ProvideName(x),
                    Type = MapType(x.ParameterType),
                    Optional = x.HasDefaultValue
                });

            string procedure = null;

            if (method.IsDefined(WampSharpAttributes.WampProcedureAttribute))
            {
                dynamic attribute = method.GetCustomAttribute(WampSharpAttributes.WampProcedureAttribute);
                procedure = attribute.Procedure;
            }
            else if (method.IsDefined(WampSharpAttributes.WampTopicAttribute))
            {
                dynamic attribute = method.GetCustomAttribute(WampSharpAttributes.WampTopicAttribute);
                procedure = attribute.Topic;
            }

            string interfaceName = mNameProvider.ProvideName(method.DeclaringType);

            return new FunctionMetadata()
            {
                Alias = mNameProvider.ProvideName(method),
                ContractName = interfaceName,
                Parameters = parameters,
                Uri = procedure,
                ReturnValueType = MapType(TaskExtensions.UnwrapReturnType(method.ReturnType)),
                EventHandler = method.IsDefined(WampSharpAttributes.WampTopicAttribute)
            };
        }

        public string MapType(Type parameterType)
        {
            if (typeof(IDictionary<,>).IsGenericAssignableFrom(parameterType))
            {
                return HandleDictionary(parameterType);
            }
            else if (parameterType.IsGenericType &&
                parameterType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return MapType(parameterType.GetGenericArguments()[0]);
            }
            else if (parameterType.IsGenericType)
            {
                return string.Format("{0}<{1}>",
                                     mNameProvider.ProvideName(parameterType.GetGenericTypeDefinition()),
                                     string.Join(", ", parameterType.GetGenericArguments().Select(x => MapType(x))));
            }
            else if (parameterType == typeof(void))
            {
                return "void";
            }
            else if (parameterType == typeof(bool))
            {
                return "boolean";
            }
            else if (parameterType == typeof(DateTime))
            {
                return "Date";
            }
            else if (parameterType == typeof(string))
            {
                return "string";
            }
            else if (parameterType.IsPrimitive)
            {
                return "number";
            }
            else if (parameterType == typeof(object))
            {
                return "any";
            }
            else if (typeof(IEnumerable<>).IsGenericAssignableFrom(parameterType))
            {
                Type elementType =
                    parameterType.GetClosedGenericTypeImplementation(typeof(IEnumerable<>))
                                 .GetGenericArguments()[0];

                string arrayType = MapType(elementType);

                return arrayType + "[]";
            }
            else
            {
                return MapCompositeType(parameterType);
            }
        }

        private string HandleDictionary(Type parameterType)
        {
            Type dictionaryType =
                parameterType.GetClosedGenericTypeImplementation(typeof(IDictionary<,>));

            Type[] genericArguments = dictionaryType.GetGenericArguments();

            string keyType = MapType(genericArguments[0]);
            string valueType = MapType(genericArguments[1]);

            if (keyType == "string")
            {
                return $"StringKeyedDictionary<{valueType}>";
            }
            else if (keyType == "number")
            {
                return $"NumberKeyedDictionary<{valueType}>";
            }
            else
            {
                throw new Exception(
                    $"Received {genericArguments[0]} keyed dictionary. Only number or string keys are supported");
            }
        }
    }
}