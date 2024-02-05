using Spectre.Console.Cli;

namespace June.Data.Commands
{
    public class TypeRegistrar : ITypeRegistrar, ITypeResolver
    {
        private readonly IServiceProvider _provider;

        public TypeRegistrar(IServiceProvider provider)
        {
            _provider = provider;
        }

        public ITypeResolver Build()
        {
            return this;
        }

        public void Register(Type service, Type implementation)
        {
            // Implement registration logic if necessary
        }

        public void RegisterInstance(Type service, object implementation)
        {
            // Implement registration logic if necessary
        }

        public void RegisterLazy(Type service, Func<object> factory)
        {
            // Implement registration logic if necessary
        }

        public object? Resolve(Type? type)
        {
            return _provider.GetService(type!);
        }
    }


}
