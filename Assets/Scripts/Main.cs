using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using static TouchHandler;

public class Main : MonoBehaviour
{
    TouchHandler touchHandler = new();
    KeyboardDraw keyboardDraw = new();
    SynthPlayer synthPlayer;

    [Min(2)]
    public int edo = 12;

    private void Awake()
    {
        synthPlayer = GetComponent<SynthPlayer>();
    }


    void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            synthPlayer.IncrementSynthIndex();
        }

        TouchList touchList = touchHandler.GetTouchList();

        for (int i = 0; i < touchList.Count; i++)
        {
            if (touchList.wasPressedThisFrame[i])
            {
                synthPlayer.AddNote(new Note(GetFrequencyFromPosition(touchList.positions[i].x), 0, synthPlayer.GetSynth()), touchList.ids[i]);
            }
            else
            {
                synthPlayer.SetNoteFrequency(touchList.ids[i], GetFrequencyFromPosition(touchList.positions[i].x));
            }
        }

        foreach (int id in synthPlayer.NoteIDList)
        {
            if (!touchList.ids.Contains(id))
            {
                synthPlayer.ReleaseNote(id);
            }
        }
    }

    float GetFrequencyFromPosition(float xPos)
    {
        return 220f.AddCents(Mathf.Lerp(0, 1200, Mathf.InverseLerp(Camera.main.ScreenToWorldPoint(new Vector2(0, 0)).x, Camera.main.ScreenToWorldPoint(new Vector2(Screen.width, 0)).x, xPos)));
    }
}
