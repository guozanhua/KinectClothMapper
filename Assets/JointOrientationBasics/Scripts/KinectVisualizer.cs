﻿//------------------------------------------------------------------------------
// <copyright file="JointMapping.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace JointOrientationBasics
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
   
    public class KinectVisualizer
        : MonoBehaviour
    {
        internal class Bone
        {
            public GameObject BoneMesh;
            public GameObject JointMesh;
        }

        /// <summary>
        /// joint model for rotation
        /// </summary>
        public GameObject JointModel;
        public float JointScale = 1;//0.03f;

        /// <summary>
        /// bone model for direction and rotation
        /// </summary>
        public GameObject BoneModel;
        public Vector3 BoneScale = Vector3.one * 1;//0.2f;
        public GameObject shirt;
        /// <summary>
        /// drawing parameters
        /// </summary>
        public bool DrawJoint = true;
        public bool DrawBoneModel = true;
        public bool DebugLines = true;
        
        /// <summary>
        /// visual skeleton of bone meshs
        /// </summary>
        private GameObject visualizerParent;
        internal GameObject VisualizerParent
        {
            get
            {
                if (this.visualizerParent == null)
                {
                    CreateSkeletonModel(Vector3.up * Helpers.FloorClipPlane.w, Quaternion.identity);
                }

                return this.visualizerParent;
            }
        }

        /// <summary>
        /// reference list of all the joint models
        /// </summary>
        private Dictionary<string, Bone> bodyModel;
        internal Dictionary<string, Bone> BodyModel
        {
            get
            {
                if (this.bodyModel == null)
                {
                    this.bodyModel = new Dictionary<string, Bone>();
                }

                return this.bodyModel;
            }
        }

        internal VisualSkeleton<Windows.Kinect.JointType> skeleton;

        // Use this for initialization
        void Start()
        {
            // if jointmapper was not set, try to find one.
            if (this.skeleton == null)
            {
                GameObject[] objs = UnityEngine.Object.FindObjectsOfType(typeof(GameObject)) as GameObject[];
                foreach (var obj in objs)
                {
                    this.skeleton = obj.GetComponent<VisualSkeleton<Windows.Kinect.JointType>>(); ;
                    if (this.skeleton != null)
                    {
                        break;
                    }
                }
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (this.skeleton == null ||
                this.JointModel == null ||
                this.BoneModel == null)
            {
                return;
            }

            Joint root = this.skeleton.GetRootJoint();
            if (root != null && this.VisualizerParent != null)
            {
                UpdateBone(root, Vector3.zero, Quaternion.identity);
            }
        }


        /// <summary>
        /// based on a joint location, will draw the bone visual for that joint
        /// </summary>
        /// <param name="joint">joint to show</param>
        private void UpdateBone(Joint joint, Vector3 parentPosition, Quaternion parentRotation)
        {
            if (joint == null)
            {
                return;
            }

            // get the rotation of the joint from Kinect
            // in the case of tip, we calculate an rotation amount
            Quaternion boneDirection = Quaternion.identity;
            if (Helpers.QuaternionZero.Equals(joint.RawRotation))
            {
                // generate a rotation if its a tip with no orientation from Kinect
                Vector3 perpendicular = Vector3.Cross(joint.LocalPosition, Vector3.up);
                Vector3 normal = Vector3.Cross(perpendicular, joint.LocalPosition);

                // calculate a rotation
                boneDirection.SetLookRotation(normal, joint.LocalPosition);
            }
            else
            {
                // Y - is the direction of the bone
                // Z - normal
                // X - bi-normal
                boneDirection = joint.Rotation;
            }

            // kinect joint identity aligns with the world so no correction needed
            Quaternion worldRotation = parentRotation * boneDirection;

            Vector3 worldPosition = parentPosition;

            // length is the distance from its parent
            float lengthOfBone = joint.LocalPosition.magnitude;

            // get the joint visual from the collection
            Bone model = this.BodyModel.FirstOrDefault(x => x.Key == joint.Name).Value;
            if (model != null)
            {
                // draw debug lines
                if (this.DebugLines)
                {
                    // visualize the rotation in world space from the parent
                    Helpers.DrawDebugBoneYDirection(parentRotation * worldPosition, lengthOfBone, worldRotation);
                }

                // update bone mesh
                GameObject boneMesh = model.BoneMesh;
                if (Helpers.SetVisible(boneMesh, this.DrawBoneModel))
                {
                    UpdateBoneMesh(boneMesh, parentRotation * worldPosition, worldRotation, lengthOfBone, this.BoneScale);
                }

                // update joint mesh
                GameObject jointMesh = model.JointMesh;
                if (Helpers.SetVisible(jointMesh, this.DrawJoint))
                {
                    UpdateJointMesh(jointMesh, parentRotation * worldPosition, worldRotation, this.JointScale);
                }
            }

            // for every child we have the corrected orientation from its parent 
            // and cacluated new position from the parent
            foreach (var child in joint.Children)
            {
                // calculate the forward direction
                Quaternion lookTo = Quaternion.identity;
                if (lengthOfBone != 0)
                {
                    lookTo = Quaternion.LookRotation(joint.LocalPosition, Vector3.up);
                }

                Vector3 toJointPosition = lookTo * Vector3.forward * lengthOfBone;

                UpdateBone(child, worldPosition + toJointPosition, parentRotation);
            }
        }

        /// <summary>
        /// create all joint models
        /// </summary>
        private void CreateSkeletonModel(Vector3 startPosition, Quaternion startRotation)
        {
            if (this.visualizerParent != null)
            {
                DestroyObject(this.VisualizerParent);
                this.visualizerParent = null;
            }

            // Create debug skeleton mapping
            this.BodyModel.Clear();

            // create parent gameObject for debug controls
            this.visualizerParent = new GameObject();
            this.visualizerParent.name = gameObject.name + "_Skeleton";
            this.visualizerParent.transform.position = startPosition;

            // create components of bone
            Joint joint = this.skeleton.GetRootJoint();
            if (joint != null)
            {
                CreateBone(joint);
            }
        }

        /// <summary>
        /// creates the bone object for the joint
        /// </summary>
        /// <param name="joint"></param>
        private void CreateBone(Joint joint)
        {
            if (joint == null)
            {
                return;
            }
            
            // create bone structure for the joint
            // need a parent to ensure the length is calculated correctly
            if(joint.Parent != null)
            {
                Bone bone = new Bone();
                // create a joint visual
                bone.JointMesh = (GameObject)Instantiate(this.JointModel);
                bone.JointMesh.name = string.Format("{0}", joint.Name);
                bone.JointMesh.transform.localScale = Vector3.one * this.JointScale;
                bone.JointMesh.gameObject.transform.parent = this.VisualizerParent.gameObject.transform;

                // create a bone visual
                bone.BoneMesh = (GameObject)Instantiate(this.BoneModel);
                bone.BoneMesh.name = string.Format("{0}", joint.Name);
                bone.BoneMesh.transform.localScale = this.BoneScale;
                bone.BoneMesh.gameObject.transform.parent = this.VisualizerParent.gameObject.transform;
                
                // add to collection for the model
                this.BodyModel.Add(joint.Name, bone);
                AddJoint(joint, bone);
            }

            // create bones for children
            foreach(var child in joint.Children)
            {
                CreateBone(child);
            }
        }

        /// <summary>
        /// Helper method to extend the bone in the direction to child
        /// </summary>
        /// <param name="bone">model used to visualize bone</param>
        /// <param name="position">start position for the model</param>
        /// <param name="rotation">the rotation to apply to the model</param>
        /// <param name="length">the distance to the child</param>
        /// <param name="boneScale">scale to apply to the model</param>
        private static void UpdateBoneMesh(GameObject bone, Vector3 position, Quaternion rotation, float length, Vector3 boneScale)
        {
            // get mesh verticies;
            MeshFilter meshFilter = (MeshFilter)bone.GetComponent("MeshFilter");
            var verticies = meshFilter.mesh.vertices;

            // get the forward vector from rotation - kinect Y is the forward
            Vector3 fwdLength = Vector3.up * length / boneScale.y; ;

            // update verticies of the tip
            // bone is oriented Y-up so no conversion needed
            verticies[4] = fwdLength;
            verticies[7] = verticies[4];
            verticies[10] = verticies[4];
            verticies[13] = verticies[4];
            meshFilter.mesh.vertices = verticies;
            meshFilter.mesh.RecalculateBounds();
            meshFilter.mesh.RecalculateNormals();

            // move bone into position
            bone.transform.position = position;
            bone.transform.rotation = rotation;
            bone.transform.localScale = boneScale;
        }

        Transform getTransform(String name)
        {
            if (shirt != null)
            {
                foreach (Transform tr in shirt.transform)
                {
                    if (tr.name.Contains(name))
                        return tr;
                }
            }
            return null;
        }
        void AddJoint(Joint joint, Bone bone)
        {
            Transform tr = null;
            
            if (joint.Name.Equals("SpineMid"))
            {
                tr = getTransform("Hips");
            }
            else if (joint.Name.Equals("SpineShoulder"))
            {
            }
            else if (joint.Name.Equals("Neck"))
            {
                //tr = getTransform("Neck");
                tr = getTransform("Spine2");
            }
            else if (joint.Name.Equals("Head"))
            {
                //tr = getTransform("");
                tr = getTransform("Neck");
            }
            else if (joint.Name.Equals("ShoulderLeft"))
            {
                tr = getTransform("RightShoulder");
            }
            else if (joint.Name.Equals("ElbowLeft"))
            {
                tr = getTransform("RightArm");
            }
            else if (joint.Name.Equals("WristLeft"))
            {
                tr = getTransform("RightForeArm");
                //tr = getTransform("");
            }
            else if (joint.Name.Equals("HandLeft"))
            {
                tr = getTransform("RightHand");
            }
            else if (joint.Name.Equals("HandTipLeft"))
            {
                tr = getTransform("HandTip");
            }
            else if (joint.Name.Equals("ThumbLeft"))
            {
                tr = getTransform("Thumb");
            }
            else if (joint.Name.Equals("ShoulderRight"))
            {
                tr = getTransform("LeftShoulder");
            }
            else if (joint.Name.Equals("ElbowRight"))
            {
                tr = getTransform("LeftArm");
            }
            else if (joint.Name.Equals("WristRight"))
            {
                tr = getTransform("LeftForeArm");
                //tr = getTransform("");
            }
            else if (joint.Name.Equals("HandRight"))
            {
                tr = getTransform("LeftHand");
            }
            else if (joint.Name.Equals("HandTipRight"))
            {
                //tr = getTransform("");
            }
            else if (joint.Name.Equals("ThumRight"))
            {
                //tr = getTransform("");
            }
            else if (joint.Name.Equals("HipLeft"))
            {
                
            }
            else if (joint.Name.Equals("KneeLeft"))
            {
                //tr = getTransform("");
            }
            else if (joint.Name.Equals("AnkleLeft"))
            {
                //tr = getTransform("");
            }
            else if (joint.Name.Equals("FootLeft"))
            {
                //tr = getTransform("");
            }
            else if (joint.Name.Equals("HipRight"))
            {
                
            }
            else if (joint.Name.Equals("KneeRight"))
            {
                //tr = getTransform("");
            }
            else if (joint.Name.Equals("AnkleRight"))
            {
                 //tr = getTransform("");
            }
            else if (joint.Name.Equals("FootRight"))
            {
                //tr = getTransform("");
            }
            if (tr != null)
            {
                Vector3 offset = Vector3.zero;
                Vector3 offsetRotation = Vector3.zero;
                if (joint.Name.Equals("Neck"))
                {
                    offset.y = -0.57f;
                }
                else if (joint.Name.Equals("ThumbLeft"))
                {
                    MeshFilter filt = bone.BoneMesh.GetComponent<MeshFilter>();
                    offset.y = filt.mesh.bounds.size.y * 3.8f;
                }
                else if(joint.Name.Equals("HandTipLeft"))
                {
                    /*MeshFilter filt = bone.BoneMesh.GetComponent<MeshFilter>();
                    offset.y = filt.mesh.bounds.size.y;*/
                }
                else if (joint.Name.Equals("SpineMid"))
                {
                    offset.y = 0.2f;
                }
                else if (joint.Name.Equals("ElbowRight") || joint.Name.Equals("ElbowLeft"))
                {
                    offset.y = 1f;
                    offsetRotation.y = 180f;
                }
                
                else if (joint.Name.Equals("ShoulderRight"))
                {
                    offset.x = 0.6f;
                    offset.y = 0.25f;
                    offsetRotation.y = -90f;
                }
                else if (joint.Name.Equals("ShoulderLeft"))
                {
                    offset.x = -0.6f;
                    offset.y = 0.25f;
                    offsetRotation.y = 90f;
                }
                //offset.z = -0.5f;
                Vector3 localPos = tr.localPosition;
                tr.parent = bone.BoneMesh.transform;
                //if(!joint.Name.Equals("ShoulderRight") || !joint.Name.Equals("ShoulderLeft"))
                resetTransform(tr, offset, offsetRotation);
                //tr.localPosition = localPos;
                
            }
            //Position of Spinebase
        }

        void resetTransform(Transform tr, Vector3 offset, Vector3 rotationOffset)
        {
            tr.localPosition = Vector3.zero + offset;
            tr.localRotation = Quaternion.Euler(rotationOffset);
        }


        /// <summary>
        /// apply transformations to the model
        /// </summary>
        /// <param name="joint">the game object to apply transformation to</param>
        /// <param name="position">the postion of the joint in world space</param>
        /// <param name="rotation">rotation to apply</param>
        /// <param name="jointScale">joint scale to adjust the size</param>
        private static void UpdateJointMesh(GameObject joint, Vector3 position, Quaternion rotation, float jointScale)
        {
            joint.transform.position = position;
            joint.transform.rotation = rotation;
            joint.transform.localScale = Vector3.one * jointScale;
        }
    }
}
