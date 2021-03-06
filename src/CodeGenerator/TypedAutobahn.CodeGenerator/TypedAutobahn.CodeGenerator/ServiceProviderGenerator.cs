using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TypedAutobahn.CodeGenerator
{
    internal class ServiceProviderGenerator
    {
        private readonly ContractMapper mMapper;
        private readonly bool mNodeJs;

        public ServiceProviderGenerator(ContractMapper mapper, bool nodeJs)
        {
            mMapper = mapper;
            mNodeJs = nodeJs;
        }

        public string GenerateProvider(Type contractType)
        {
            string serviceName = mMapper.MapType(contractType);

            string subscriberArguments =
                GetArguments(contractType, WampSharpAttributes.WampTopicAttribute);

            string calleeArguments =
                GetArguments(contractType, WampSharpAttributes.WampProcedureAttribute);

            string result = $@"class {serviceName}Provider extends RealmServiceProviderBase implements IContractRealmServiceProvider<{serviceName}, {serviceName}Proxy> {{
    constructor(session: autobahn.Session) {{
        super(session);
    }}

    public registerCallee(instance: {serviceName}): When.Promise<autobahn.IRegistration[]> {{
        return super.registerMethodsAsCallee({calleeArguments});
    }}

    registerSubscriber(instance: {serviceName}): When.Promise<autobahn.ISubscription[]> {{
        return super.registerMethodsAsSubscriber({subscriberArguments});
    }}

    getCalleeProxy(): {serviceName}Proxy {{
        return new {serviceName}ProxyImpl(this._session);
    }}
}}";

            if (mNodeJs)
            {
                result = "export " + result;
            }

            return result;
        }

        private string GetArguments(Type contractType, Type attributeType)
        {
            string serviceType = mMapper.MapType(contractType);

            IEnumerable<string> subscriberArguments =
                contractType.GetMethods()
                            .Where(x => x.IsDefined(attributeType))
                            .Select(x => mMapper.MapMethod(x))
                            .Select(x => x.Alias);

            IEnumerable<string> allArguments =
                new[] {"instance"}.Concat
                    (subscriberArguments.Select(method => $"{serviceType}Metadata.{method}"));

            const string separator = @",
            ";

            return string.Join(separator, allArguments);
        }
    }
}