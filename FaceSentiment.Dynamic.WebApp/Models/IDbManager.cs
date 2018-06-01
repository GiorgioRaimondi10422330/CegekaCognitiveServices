using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.ProjectOxford.Face.Contract;

namespace FaceSentiment.Dynamic.WebApp.Models
{
    public interface IDbManager
    {
        Task<bool> DeleteCamera(CameraValues camera);
        void DeleteCamera(CameraValues camera, string filePath);
        Task<List<CameraValues>> ReadCamera();
        List<CameraValues> ReadCamera(string filePath);
        Connections ReadConnections(string filePath);
        DetectionSetting ReadDetections(string filePath);
        void ReadPerson(PersonSeenList people);
        Task<bool> SaveFaceSentiment(Face[] faces, string cameraId, DateTime date);
        void SavePerson(Guid faceId, Guid personId, DateTime date);
        void SetConnection(Connections conn);
        Task<bool> UpdateCamera(CameraValues camera);
        void UpdateCamera(CameraValues camera, string filePath);
        void UpdateConnections(Connections connections, string filePath);
        void UpdateDetections(DetectionSetting detection, string filePath);
    }
}