/*
 * DTWRecognizer.cs
 *
 * Implements IModel using true Dynamic Time Warping over sequences.
 * Inputs are expected to be flattened frame sequences:
 *
 * [frame0_feature0, frame0_feature1, ..., frame1_feature0, frame1_feature1, ...]
 *
 * frameSize defines how many floats belong to one frame.
 */

using System.Collections.Generic;
using UnityEngine;

namespace RTMLToolKit
{
    /// <summary>
    /// Dynamic Time Warping recogniser for sequence classification/regression.
    /// Each input sample is a flattened sequence of frames.
    /// </summary>
    public class DTWRecognizer : IModel
    {
        /// <summary>Number of features expected per full flattened input sequence.</summary>
        public int inputSize;

        /// <summary>Number of dimensions in the output vector.</summary>
        public int outputSize;

        /// <summary>Number of feature values per frame.</summary>
        public int frameSize;

        /// <summary>
        /// Required similarity score between 0 and 1.
        /// Higher values require a closer match.
        /// </summary>
        public float similarityThreshold;

        /// <summary>Stored template gesture sequences.</summary>
        public List<float[]> templates;

        /// <summary>Stored outputs corresponding to each template sequence.</summary>
        public List<float[]> templateOutputs;

        /// <summary>
        /// Constructor: specify full input size, output size, and per-frame feature size.
        /// </summary>
        public DTWRecognizer(int inputSize, int outputSize, int frameSize = 1)
        {
            this.inputSize = inputSize;
            this.outputSize = outputSize;
            this.frameSize = Mathf.Max(1, frameSize);
            this.similarityThreshold = 0.8f;
            this.templates = new List<float[]>();
            this.templateOutputs = new List<float[]>();
        }

        /// <summary>
        /// Stores each input/output pair as a DTW template.
        /// </summary>
        public void Train(List<float[]> inputs, List<float[]> outputs)
        {
            if (inputs.Count != outputs.Count)
            {
                Logger.LogWarning("[DTWRecognizer] Input/output count mismatch.");
                return;
            }

            templates.Clear();
            templateOutputs.Clear();

            for (int i = 0; i < inputs.Count; i++)
            {
                if (inputs[i] == null || outputs[i] == null)
                {
                    Logger.LogWarning($"[DTWRecognizer] Skipping null sample at index {i}.");
                    continue;
                }

                if (inputs[i].Length < frameSize)
                {
                    Logger.LogWarning($"[DTWRecognizer] Skipping sample at index {i}; input length is smaller than frame size.");
                    continue;
                }

                templates.Add((float[])inputs[i].Clone());
                templateOutputs.Add((float[])outputs[i].Clone());
            }

            Logger.Log($"[DTWRecognizer] Stored {templates.Count} DTW template sequences.");
        }

        /// <summary>
        /// Predicts by finding the template sequence with the lowest DTW distance.
        /// Returns the template output only if the similarity meets the threshold.
        /// </summary>
        public float[] Predict(float[] input)
        {
            if (templates.Count == 0)
            {
                Logger.LogWarning("[DTWRecognizer] No templates available to predict from.");
                return new float[outputSize];
            }

            if (input == null || input.Length < frameSize)
            {
                Logger.LogWarning("[DTWRecognizer] Input is null or too short.");
                return new float[outputSize];
            }

            float bestDistance = float.PositiveInfinity;
            int bestIndex = -1;

            for (int i = 0; i < templates.Count; i++)
            {
                float distance = CalculateDtwDistance(input, templates[i]);

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestIndex = i;
                }
            }

            float similarity = DistanceToSimilarity(bestDistance);

            if (bestIndex != -1 && similarity >= similarityThreshold)
            {
                return (float[])templateOutputs[bestIndex].Clone();
            }

            return new float[outputSize];
        }

        /// <summary>
        /// Calculates true DTW distance between two flattened frame sequences.
        /// </summary>
        private float CalculateDtwDistance(float[] sequenceA, float[] sequenceB)
        {
            int frameCountA = sequenceA.Length / frameSize;
            int frameCountB = sequenceB.Length / frameSize;

            if (frameCountA == 0 || frameCountB == 0)
            {
                return float.PositiveInfinity;
            }

            float[,] dtw = new float[frameCountA + 1, frameCountB + 1];

            for (int i = 0; i <= frameCountA; i++)
            {
                for (int j = 0; j <= frameCountB; j++)
                {
                    dtw[i, j] = float.PositiveInfinity;
                }
            }

            dtw[0, 0] = 0f;

            for (int i = 1; i <= frameCountA; i++)
            {
                for (int j = 1; j <= frameCountB; j++)
                {
                    float frameDistance = CalculateFrameDistance(sequenceA, i - 1, sequenceB, j - 1);

                    float previousBest = Mathf.Min(
                        dtw[i - 1, j],
                        Mathf.Min(dtw[i, j - 1], dtw[i - 1, j - 1])
                    );

                    dtw[i, j] = frameDistance + previousBest;
                }
            }

            int pathNormalisationLength = frameCountA + frameCountB;
            return dtw[frameCountA, frameCountB] / Mathf.Max(1, pathNormalisationLength);
        }

        /// <summary>
        /// Euclidean distance between two frames inside flattened sequences.
        /// </summary>
        private float CalculateFrameDistance(float[] sequenceA, int frameIndexA, float[] sequenceB, int frameIndexB)
        {
            int offsetA = frameIndexA * frameSize;
            int offsetB = frameIndexB * frameSize;

            float sumSq = 0f;

            for (int feature = 0; feature < frameSize; feature++)
            {
                int indexA = offsetA + feature;
                int indexB = offsetB + feature;

                if (indexA >= sequenceA.Length || indexB >= sequenceB.Length)
                {
                    break;
                }

                float difference = sequenceA[indexA] - sequenceB[indexB];
                sumSq += difference * difference;
            }

            return Mathf.Sqrt(sumSq);
        }

        /// <summary>
        /// Converts distance to a 0-1 similarity score.
        /// A perfect match gives 1.
        /// Larger distances approach 0.
        /// </summary>
        private float DistanceToSimilarity(float distance)
        {
            if (float.IsInfinity(distance) || float.IsNaN(distance))
            {
                return 0f;
            }

            return 1f / (1f + distance);
        }
    }
}