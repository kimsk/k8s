[pods](https://kubernetesbyexample.com/pods/)

> Containers are designed to run only a single process per container

> In k8s, container is deployed and run inside the pod.

> A pod is lightweight , and a pod with one container is common.

> k8s scales the whole pod (not individual container).

> A pod should contain one main container and helper containers work tightly together.

> Containers in a pod run on a single worker node and share the same ip address & port space.

> All pods in a Kubernetes cluster reside in a single flat, shared, network-address space, no matter which worker node they are in.

> One pod can communicate with another pod via ip address no matter which worker node they are in.

> Pods are logical hosts and behave much like physical hosts or VMs in the non-container world.

[Eight Ways to Create a Pod](https://www.cyberark.com/resources/threat-research-blog/eight-ways-to-create-a-pod)
