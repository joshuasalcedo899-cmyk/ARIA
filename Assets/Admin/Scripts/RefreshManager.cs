using UnityEngine;
using UnityEngine.SceneManagement;

public class AppReloader : MonoBehaviour
{
    public void RefreshApp()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
