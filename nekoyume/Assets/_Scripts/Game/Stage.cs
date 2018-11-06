using System.Collections;
using UnityEngine;
using UnityEngine.UI;


namespace Nekoyume.Game
{
    public class Stage : MonoBehaviour
    {
        public FollowCamera followCam = null;
        public GameObject avatar;
        public GameObject background = null;

        private string zone;

        public void Start()
        {
            InitCamera();
            LoadBackground("nest");
        }

        public void InitCamera()
        {
            followCam = Camera.main.gameObject.GetComponent<FollowCamera>();
            if (followCam == null)
            {
                followCam = Camera.main.gameObject.AddComponent<FollowCamera>();
            }
        }

        public void LoadBackground(string prefabName)
        {
            if (background != null)
            {
                if (background.name.Equals(prefabName))
                {
                    return;
                }
                Destroy(background);
                background = null;
            }
            string resName = string.Format("Prefab/Background/{0}", prefabName);
            GameObject prefab = Resources.Load<GameObject>(resName);
            if (prefab != null)
            {
                background = Instantiate(prefab, transform);
                background.name = prefabName;
            }
            var camPosition = followCam.transform.position;
            camPosition.x = 0;
            followCam.transform.position = camPosition;
        }

        public void OnLogin(Network.Response.Login response)
        {
            LoadBackground("room");
            zone = response.avatar.zone;
            var character = avatar.GetComponent<Character>();
            StartCoroutine(character.Load(avatar, response.avatar.class_));
            UI.Widget.Create<UI.Move>().Show();
        }

        public void Move()
        {
            var character = avatar.GetComponent<Character>();
            StartCoroutine(character.Walk());
            followCam.target = avatar.transform;
            LoadBackground(zone);
        }

        public void OnHackAndSlash(Network.Response.LastStatus response)
        {
            var character = avatar.GetComponent<Character>();
            StartCoroutine(character.Stop());
            StartCoroutine(ProcessStatus(response));
        }

        private IEnumerator ProcessStatus(Network.Response.LastStatus lastStatus)
        {
            foreach (var battleStatus in lastStatus.status)
            {
                var statusType = battleStatus.GetStatusType();
                if (statusType != null)
                {
                    var status = (Status.Base)System.Activator.CreateInstance(statusType);
                    if (status != null)
                    {
                        yield return status.Execute(this, battleStatus);
                    }
                }
            }
        }

        public void Home()
        {

        }
    }
}
