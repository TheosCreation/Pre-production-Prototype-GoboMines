using UnityEngine;

[CreateAssetMenu(fileName = "TransformDataStorage", menuName = "ScriptableObjects/TransformDataStorage", order = 1)]
public class TransformDataStorage : ScriptableObject
{
    [System.Serializable]
    public class TransformData
    {
        public Vector3 localPosition;
        public Quaternion localRotation;
    }

    // Arrays to store hand IK transforms
    public TransformData[] leftHandTransforms = new TransformData[3];
    public TransformData[] rightHandTransforms = new TransformData[3];

    // Method to save local hand transforms
    public void SaveLocalHandTransforms(Transform[] leftHand, Transform[] rightHand)
    {
        for (int i = 0; i < leftHand.Length; i++)
        {
            leftHandTransforms[i].localPosition = leftHand[i].localPosition;
            leftHandTransforms[i].localRotation = leftHand[i].localRotation;
        }

        for (int i = 0; i < rightHand.Length; i++)
        {
            rightHandTransforms[i].localPosition = rightHand[i].localPosition;
            rightHandTransforms[i].localRotation = rightHand[i].localRotation;
        }
    }

    // Method to load local hand transforms
    public void LoadLocalHandTransforms(Transform[] leftHand, Transform[] rightHand)
    {
        for (int i = 0; i < leftHand.Length; i++)
        {
            leftHand[i].localPosition = leftHandTransforms[i].localPosition;
            leftHand[i].localRotation = leftHandTransforms[i].localRotation;
        }

        for (int i = 0; i < rightHand.Length; i++)
        {
            rightHand[i].localPosition = rightHandTransforms[i].localPosition;
            rightHand[i].localRotation = rightHandTransforms[i].localRotation;
        }
    }
}
