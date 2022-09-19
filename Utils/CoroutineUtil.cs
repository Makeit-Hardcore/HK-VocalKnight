using System.Collections;
using UnityEngine;

namespace VocalKnight.Utils
{
	public class CoroutineUtil
	{
		public static bool cancel = false;

        public static IEnumerator WaitWithCancel(float waitTime)
        {
            cancel = false;
            for (float timer = waitTime; timer > 0; timer -= Time.deltaTime)
            {
                if (cancel) yield break;
                yield return null;
            }
        }
    }
}
