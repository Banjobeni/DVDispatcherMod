using System;
using DVDispatcherMod.DispatcherHints;

namespace DVDispatcherMod.DispatcherHintShowers {
    public interface IDispatcherHintShower : IDisposable {
        void SetDispatcherHint(DispatcherHint dispatcherHintOrNull);
    }
}