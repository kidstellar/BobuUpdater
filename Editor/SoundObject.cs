namespace BobuEditor
{
    using UnityEngine;

    //M�zikler i�in kullan�lan alan
    [CreateAssetMenu(fileName = "NewSound", menuName = "Sound Library/Sound")]
    public class SoundObject : ScriptableObject
    {
        public string soundName;
        public string downloadLink;
        public SoundTag tag;        // Art�k bir liste de�il, tek se�im
        public SubSoundTag subTag;  // Alt t�r
    }

    public enum SoundTag
    {
        All,
        Ambient,
        Music,
        SFX,
        Voice
    }

    public enum SubSoundTag
    {
        None,           // E�er alt t�r kullan�lmayacaksa
        AI,
        Agriculture,
        Animal,
        Button,
        CartoonMinionFussin,
        CharacterMove,
        Home,
        MotorVehicle,
        OverallSound,
        Piano,
        Sport,
        Water,
        Weather,
        AnimalsInfo,
        BigMax,
        Colors,
        Pasha,
        Pika,
        Rose,
        Toys
    } 
}