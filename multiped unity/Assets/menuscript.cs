using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class menuscript : MonoBehaviour
{
    public void StartBtn()
    {
       
       // FileUtility.WriteState(1);
        SceneManager.LoadScene("Environment");
    }
    public void ExitBtn()
    {
        Application.Quit();
    }
}
