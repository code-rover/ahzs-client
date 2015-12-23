using UnityEngine;
using System.Collections;

public class FadeOccluer : MonoBehaviour
{

    Transform m_myTransform;
    TweenAlpha m_ta;
    Material m_matAlphaOccluer;
    Material m_matOld;
    float m_fFadeStart = 1;
    float m_fFadeEnd = 0;
    bool m_isStartFade = false;
    float m_fFadeSpeed = 1;
    const float FadeSpeed = 1;

    void Awake()
    {
        m_myTransform = transform;

        if (renderer == null)
            return;

        m_matOld = renderer.material;

        if (m_matAlphaOccluer == null)
        {
            AssetCacheMgr.GetUIResource("MogoFadeOccluer.mat", (go) =>
            {
                Material mat = go as Material;

                mat.mainTexture = renderer.material.mainTexture;

                m_matAlphaOccluer = mat;

                renderer.material = m_matAlphaOccluer;

                TrulyStartFadeOut();
            });
        }
    }

    private void TrulyStartFadeOut()
    {
        m_fFadeStart = 1;
        m_fFadeEnd = 0;
        m_fFadeSpeed = -FadeSpeed;
        m_isStartFade = true;
    }


    public void StartFadeOut()
    {
        if (m_matAlphaOccluer != null)
        {
            m_matAlphaOccluer.mainTexture = renderer.material.mainTexture;

            renderer.material = m_matAlphaOccluer;

            //if (m_matOld.color != null)
            //{
            //    renderer.material.color = m_matOld.color;
            //}
            //else
            //{
            //    renderer.material.color = new Color(1, 1, 1, 1);
            //}

            renderer.material.color = new Color(1, 1, 1, 1);

            m_fFadeStart = 1;
            m_fFadeEnd = 0;

            m_isStartFade = true;
        }
        else
        {

            m_fFadeStart = 1;
            m_fFadeEnd = 0;
        }

        m_fFadeSpeed = -FadeSpeed;
    }

    public void StartFadeIn()
    {
        m_fFadeStart = 0;
        m_fFadeEnd = 1;

        m_isStartFade = true;

        m_fFadeSpeed = FadeSpeed;
    }

    void Update()
    {

        if (m_isStartFade)
        {
            Color currentColor = renderer.material.color;
            float fadeNum = currentColor.a + m_fFadeSpeed * Time.deltaTime;
           
            renderer.material.color = new Color(currentColor.r, currentColor.g, currentColor.b, fadeNum);

            if (Mathf.Abs(m_fFadeEnd - fadeNum) < 0.01f)
            {
                renderer.material.color = new Color(currentColor.r, currentColor.g, currentColor.b, m_fFadeEnd);

                if (m_fFadeEnd > 0.5f)
                {
                    renderer.material = m_matOld;
                }

                m_isStartFade = false;
            }
        }

    }
}
