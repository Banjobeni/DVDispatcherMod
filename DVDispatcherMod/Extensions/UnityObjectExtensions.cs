namespace DVDispatcherMod.Extensions {
    public static class UnityObjectExtensions {
        public static void DestroyIfAlive(this UnityEngine.Object obj) {
            // unity implements an implicit conversion to bool that returns false if the unmanaged object has been destroyed
            var isObjectAlive = (bool)obj;
            if (isObjectAlive) {
                UnityEngine.Object.Destroy(obj);
            }
        }
    }
}