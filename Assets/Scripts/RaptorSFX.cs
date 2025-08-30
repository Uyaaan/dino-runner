using UnityEngine;

public class RaptorSFX : MonoBehaviour
{
    [SerializeField] private AudioSource src;
    [SerializeField] private AudioClip growl;

    // called by Animation Event on the clip
    public void Growl()
    {
        if (src && growl) src.PlayOneShot(growl);
        // If you just want to silence the event, leave this empty instead.
    }
}