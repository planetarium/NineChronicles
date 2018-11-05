using System.Collections;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;


namespace Nekoyume.Game
{
    public class Stage : MonoBehaviour
    {
        public FollowCamera followCam = null;
        public GameObject avatar;
        public GameObject background = null;
        public GameObject joinModal;
        public Text txtMessage;
        public Button btnLogin;
        public Button btnMove;
        public Button btnHome;

        private string zone;

        public void Start()
        {
            InitCamera();

            btnMove.gameObject.SetActive(false);
            btnHome.gameObject.SetActive(false);
            joinModal.gameObject.SetActive(false);
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

        public void Login()
        {
            btnLogin.gameObject.SetActive(false);
            Network.NetworkManager networkInstance = Network.NetworkManager.Instance;

            if (string.IsNullOrEmpty(networkInstance.privateKey))
            {
                networkInstance.privateKey = networkInstance.GeneratePrivateKey();
                PlayerPrefs.SetString("private_key", networkInstance.privateKey);
                joinModal.gameObject.SetActive(true);
            }
            else
            {
                networkInstance.Push(new Network.Request.Login() {
                    ResponseCallback = OnLogin
                });
            }
        }

        public void OnLogin(Network.Response.Login response)
        {
            if (response.result == Network.ResultCode.OK)
            {
                btnMove.gameObject.SetActive(true);
                LoadBackground("room");
                zone = response.avatar.zone;
                var character = avatar.GetComponent<Character>();
                StartCoroutine(character.Load(response.avatar.class_));
            }
            else
            {
                Debug.Log(response.message);
                joinModal.gameObject.SetActive(true);
            }
        }

        public void Move()
        {
            var character = avatar.GetComponent<Character>();
            StartCoroutine(character.Walk());
            followCam.target = avatar.transform;
            LoadBackground(zone);
            btnMove.gameObject.SetActive(false);
            Network.NetworkManager networkInstance = Network.NetworkManager.Instance;
            new Network.Request.SessionMoves() {
                name = "hack_and_slash",
                ResponseCallback = OnHackAndSlash
            }.Send();
        }

        public void OnHackAndSlash(Network.Response.LastStatus response)
        {
            btnHome.gameObject.SetActive(true);
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

        public void Join()
        {
            joinModal.gameObject.SetActive(false);
            Network.NetworkManager networkInstance = Network.NetworkManager.Instance;
            var nameField = joinModal.gameObject.GetComponentInChildren<InputField>();
            networkInstance.Push(new Network.Request.Join()
            {
                name = nameField.text,
                ResponseCallback = OnLogin
            });
        }
    }
}
