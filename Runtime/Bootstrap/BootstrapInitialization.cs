using System.Collections;

namespace INLO.Core.Bootstrap
{
    public interface IBootstrapInitializer
    {
        IEnumerator Initialize(BootstrapInitializationContext context);
    }

    public sealed class BootstrapInitializationContext
    {
        public bool Succeeded { get; private set; } = true;
        public string FailureMessage { get; private set; }

        public void Fail(string message)
        {
            Succeeded = false;
            FailureMessage = message;
        }
    }
}
