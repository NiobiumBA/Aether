using System;

namespace Aether
{
    public static class NetworkEventSystem
    {
        /// <summary>
        /// Invokes in LateUpdate before invoking HandleData.
        /// Therefore, do not send data in LateUpdate.
        /// </summary>
        public static event Action SendData;

        public static event Action BeforeHandleData;

        /// <summary>
        /// Invokes in LateUpdate after invoking SendData.
        /// Therefore, do not send data in LateUpdate.
        /// </summary>
        public static event Action HandleData;

        public static void InvokeSendData()
        {
            SendData?.Invoke();
        }

        public static void InvokeBeforeHandleData()
        {
            BeforeHandleData?.Invoke();
        }

        public static void InvokeHandleData()
        {
            HandleData?.Invoke();
        }
    }
}