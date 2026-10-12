using UnityEngine;

public class BookOpener : MonoBehaviour
{
    public Transform[] pages;  // Array of pages to open
    public float[] pageRotationAngles; // Rotation angles for each page

    private bool isOpening = false;
    private bool isClosing = false;

    void Start()
    {
        // Initialize pageRotationAngles array based on initial rotations of pages
        pageRotationAngles = new float[pages.Length];
        for (int i = 0; i < pages.Length; i++)
        {
            pageRotationAngles[i] = pages[i].localEulerAngles.y;
        }
    }

    void Update()
    {
        // Check for left mouse button click to open the book
        if (Input.GetMouseButtonDown(1))
        {
            OpenBook();
        }

        // Check for right mouse button click to close the book
        if (Input.GetMouseButtonDown(0))
        {
            CloseBook();
        }

        if (isOpening)
        {
            bool allOpened = true;
            for (int i = 0; i < pages.Length; i++)
            {
                if (pageRotationAngles[i] < 45.0f)
                {
                    pageRotationAngles[i] += Time.deltaTime * 45.0f;
                    pages[i].localEulerAngles = new Vector3(0, pageRotationAngles[i], 0);
                    allOpened = false;
                }
            }
            if (allOpened)
            {
                isOpening = false;
            }
        }

        if (isClosing)
        {
            bool allClosed = true;
            for (int i = 0; i < pages.Length; i++)
            {
                if (pageRotationAngles[i] > 0.0f)
                {
                    pageRotationAngles[i] -= Time.deltaTime * 45.0f;
                    pages[i].localEulerAngles = new Vector3(0, pageRotationAngles[i], 0);
                    allClosed = false;
                }
            }
            if (allClosed)
            {
                isClosing = false;
            }
        }
    }

    public void OpenBook()
    {
        if (!isOpening && !isClosing)
        {
            isOpening = true;
        }
    }

    public void CloseBook()
    {
        if (!isOpening && !isClosing)
        {
            isClosing = true;
        }
    }
}