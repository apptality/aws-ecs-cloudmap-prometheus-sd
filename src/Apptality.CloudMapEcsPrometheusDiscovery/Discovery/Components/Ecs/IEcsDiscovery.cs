using Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.Ecs.Models;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Components.Ecs;

public interface IEcsDiscovery
{
    /// <summary>
    /// Resolves the ECS clusters information provided via configuration options
    /// </summary>
    /// <returns>
    /// List of ECS clusters information
    /// </returns>
    Task<ICollection<EcsCluster>> GetClusters(ICollection<string> clusterNames);

    /// <summary>
    /// Resolves the ECS services information for a given cluster
    /// </summary>
    /// <returns>
    /// List of services information for the given cluster
    /// </returns>
    Task<EcsClusterServices> GetClusterServices(string clusterArn);

    /// <summary>
    /// Resolves the ECS services information for a given cluster and service connect namespace
    /// </summary>
    /// <returns>
    /// List of services information for the given cluster and service connect namespace
    /// </returns>
    Task<EcsCloudMapServices> GetCloudMapServices(string namespaceArn);

    /// <summary>
    /// Describes the ECS services for the given service ARNs
    /// </summary>
    /// <returns>
    /// List of services descriptions for the given service ARNs
    /// </returns>
    Task<ICollection<EcsService>> DescribeServices(ICollection<string> serviceArns);

    /// <summary>
    /// Returns information about running ECS tasks for the given cluster and service
    /// </summary>
    /// <returns>
    /// List of running tasks definitions for the given cluster and service
    /// </returns>
    Task<EcsServiceTasks> GetRunningTasks(string clusterArn, string serviceArn);

    /// <summary>
    /// Describes the ECS tasks for the given cluster tasks ARNs
    /// </summary>
    /// <returns>
    /// List of tasks definitions for the given cluster and tasks ARNs
    /// </returns>
    Task<ICollection<EcsTask>> DescribeTasks(string clusterArn, string serviceArn, string[] taskArns);
}