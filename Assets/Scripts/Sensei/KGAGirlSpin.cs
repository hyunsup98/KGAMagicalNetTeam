using UnityEngine;
using System.Collections;

public class KGAGirlSpin : MonoBehaviour
{
    [SerializeField] float spinSpeed = 15f;
    [SerializeField] float upAndDownSpeed = 1f;
    [SerializeField] float upAndDownDistance = 0.5f;

    Coroutine spin;
    Coroutine upAndDown;
    float topY;
    float bottomY;
    bool goingUp = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    IEnumerator Start()
    {
        yield return null;
        topY = transform.localPosition.y + 0.5f;
        bottomY = transform.localPosition.y - 0.5f;

        while (gameObject.activeSelf)
        {
            if (goingUp)
            {
                transform.localPosition += Vector3.up * upAndDownSpeed * Time.deltaTime;

                if (transform.localPosition.y >= topY)
                {
                    goingUp = false;
                }


            }
            else
            {
                transform.localPosition += Vector3.down * upAndDownSpeed * Time.deltaTime;
                if (transform.localPosition.y <= bottomY)
                {
                    goingUp = true;
                }
            }

            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime);

            yield return null;


        }

        yield return null;
    }

    
    
    
    // Update is called once per frame


    void Update()
    {
        
    }
}
