using UnityEngine;
using System.Collections;
using Mogo.Util;

public class ChooseCharacterUIBG : MonoBehaviour
{
    void OnClick()
    {
        if (Camera.mainCamera)
        {
            RaycastHit m_castResult;
            Ray m_mouseRay = Camera.mainCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(m_mouseRay, out m_castResult, 1000, (int)Mogo.Util.LayerMask.Character))
            {
                EventDispatcher.TriggerEvent<GameObject>("NewLoginUIViewManager.CreateCharacterChooseModel", m_castResult.collider.gameObject);
            }
        }
    }
}
