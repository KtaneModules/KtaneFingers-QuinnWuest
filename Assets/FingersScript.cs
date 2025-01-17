using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class FingersScript : MonoBehaviour
{
    public KMNeedyModule Needy;
    public KMBombInfo BombInfo;
    public KMAudio Audio;

    public KMSelectable[] ButtonSels;
    public SpriteRenderer FingerSpriteRenderer;
    public Sprite[] FingerSprites;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _isActivated;

    private int _current;
    private int _previous;

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        Needy.OnNeedyActivation += Activate;
        Needy.OnNeedyDeactivation += Deactivate;

        Needy.OnTimerExpired += delegate ()
        {
            Debug.LogFormat("[Fingers #{0}] Ran out of time. Strike.", _moduleId);
            Needy.HandleStrike();
            Deactivate();
        };

        for (int i = 0; i < ButtonSels.Length; i++)
            ButtonSels[i].OnInteract += ButtonPress(i);

        _previous = -1;
        _current = Rnd.Range(0, 5);
        FingerSpriteRenderer.sprite = FingerSprites[_current];
        Debug.LogFormat("[Fingers #{0}] The needy shows {1} fingers.", _moduleId, _current + 1);
    }

    private void Activate()
    {
        _isActivated = true;
        _previous = _current;
        _current = Rnd.Range(0, 5);
        FingerSpriteRenderer.sprite = FingerSprites[_current];
        Debug.LogFormat("[Fingers #{0}] The needy shows {1} fingers. The correct button to press is {2}.", _moduleId, _current + 1, _previous + 1);
    }

    private void Deactivate()
    {
        _isActivated = false;
        Needy.HandlePass();
    }

    private KMSelectable.OnInteractHandler ButtonPress(int i)
    {
        return delegate ()
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, ButtonSels[i].transform);
            ButtonSels[i].AddInteractionPunch(0.5f);
            if (!_isActivated)
                return false;
            if (_previous == i)
            {
                Debug.LogFormat("[Fingers #{0}] Correctly pressed {1}.", _moduleId, i + 1);
                Needy.HandlePass();
                Deactivate();
            }
            else
            {
                Debug.LogFormat("[Fingers #{0}] Incorrectly pressed {1}, when {2} was expected. Strike.", _moduleId, i + 1, _previous + 1);
                Deactivate();
                Needy.HandleStrike();
            }
            return false;
        };
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} press 1 [Press the 1 button.] | 'press' is optional.";
#pragma warning restore 414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        var m = Regex.Match(command, @"^\s*((press|submit)\s+)?(?<num>[12345])\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!m.Success)
            yield break;
        yield return null;
        ButtonSels[int.Parse(m.Groups["num"].Value) - 1].OnInteract();
    }

    private void TwitchHandleForcedSolve()
    {
        StartCoroutine(Autosolve());
    }

    private IEnumerator Autosolve()
    {
        while (true)
        {
            while (_isActivated)
                yield return true;
            ButtonSels[_previous].OnInteract();
        }
    }
}