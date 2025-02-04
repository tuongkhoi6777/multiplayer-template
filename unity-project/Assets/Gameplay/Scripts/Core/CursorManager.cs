using Core;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class CursorManager : MonoBehaviour
{
    void Awake()
    {
        EventManager.emitter.On(EventManager.UPDATE_CURSOR, UpdateCursor);
    }

    public void UpdateCursor(object[] args)
    {
        var jobject = JObject.FromObject(args[0]);
        bool isShow = jobject["isShow"].Value<bool>();

        // hide cursor if controlling character, else show it
        if (isShow)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}