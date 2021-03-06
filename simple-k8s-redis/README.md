# kind

## set up multi-nodes k8s
```
kind create cluster --name simple-redis --config .\kind-multi-nodes.yaml
```

## check
```
kubectl config current-context


kind-simple-redis
```

```
kind get nodes --name simple-redis

kind-control-plane
kind-worker
kind-worker3
kind-worker2

kubectl get nodes

NAME                         STATUS   ROLES    AGE     VERSION
simple-redis-control-plane   Ready    master   6m56s   v1.19.1
simple-redis-worker          Ready    <none>   6m30s   v1.19.1
simple-redis-worker2         Ready    <none>   6m31s   v1.19.1
simple-redis-worker3         Ready    <none>   6m31s   v1.19.1
```

# Minikube
```
minikube start
```


# redis

## create storage &logging namespace
```
kubectl apply -f ./namespaces.yaml

namespace/logging created
namespace/storage created

kubectl get ns

NAME                 STATUS   AGE
default              Active   10m
kube-node-lease      Active   10m
kube-public          Active   10m
kube-system          Active   10m
local-path-storage   Active   10m
logging              Active   4s
storage              Active   4s
```

## create redis-master pod & service
```
kubectl apply -f ./redis-pod.yaml

pod/redis-master created
service/redis created

kubectl get pods --namespace storage

NAME           READY   STATUS    RESTARTS   AGE
redis-master   1/1     Running   0          24s

kubectl get svc --namespace storage

NAME    TYPE        CLUSTER-IP     EXTERNAL-IP   PORT(S)    AGE
redis   ClusterIP   10.96.183.64   <none>        6379/TCP   11s
```

### testing with rdcli (npm install -g redis-cli)
```
kubectl port-forward --namespace storage redis-master 6379:6379

rdcli ping
PONG
```

## create redis-master deployment & service (if not as a pod)
```
kubectl apply -f ./redis-deployment.yaml

deployment.apps/redis-master created
service/redis created

kubectl get deploy -n storage

NAME           READY   UP-TO-DATE   AVAILABLE   AGE
redis-master   1/1     1            1           16s

kubectl get po -n storage

NAME                          READY   STATUS    RESTARTS   AGE
redis-master-d9d49b7f-rrqck   1/1     Running   0          21s

kubectl get svc -n storage

NAME    TYPE        CLUSTER-IP      EXTERNAL-IP   PORT(S)    AGE
redis   ClusterIP   10.96.244.218   <none>        6379/TCP   28s
```



## create rdcli pod
```
kubectl apply -f ./rdcli-pods.yaml

# or deployments
kubectl apply -f ./rdcli-deployment.yaml


kubectl get pods

NAME    READY   STATUS    RESTARTS   AGE
rdcli   1/1     Running   0          3m28s

kubectl exec -it rdcli -c rdcli1 -- rdcli -h redis.storage ping

PONG

kubectl exec -it rdcli -c rdcli1 -- rdcli -h redis.storage rpush items "apple" "banana" "cherry" "durian"
(integer) 4

kubectl exec -it rdcli -c rdcli1 -- rdcli -h redis.storage keys *

1) jobs

kubectl exec -it rdcli -c rdcli1 -- rdcli -h redis.storage lrange items 0 -1

1) apple
2) banana
3) cherry
4) durian
```

## create cronjob to count # of jobs
```
kubectl apply -f ./count-items-cronjob.yaml

cronjob.batch/count-items created

kubectl get cronjob -n logging

NAME          SCHEDULE      SUSPEND   ACTIVE   LAST SCHEDULE   AGE
count-items   */5 * * * *   False     0        <none>          73s

kubectl get pods -n logging

NAME                           READY   STATUS      RESTARTS   AGE
count-items-1605020700-bmdcd   0/1     Completed   0          3m40s

kubectl logs count-items-1605020700-bmdcd

(integer) 4
```

## test F# redis-job
```
kubectl run -i --tty --rm redis-job --image karlkim/redis-job -- ./redis-job --jobname test-job

If you don't see a command prompt, try pressing enter.
found item: apple..
redis-job done...
Session ended, resume using 'kubectl attach redis-job -c redis-job -i -t' command when the pod is running
pod "redis-job" deleted
```

## create F# redis-job
```
kubectl apply -f ./redis-job.yaml

kubectl get pods

NAME              READY   STATUS      RESTARTS   AGE
rdcli             2/2     Running     0          56m
redis-job-58pjw   0/1     Completed   0          45s
redis-job-9wc4p   0/1     Completed   0          39s
redis-job-dvzxf   0/1     Completed   0          50s
redis-job-h9pvz   0/1     Completed   0          50s
redis-job-js7hx   0/1     Completed   0          50s
redis-job-k7d6s   0/1     Completed   0          43s

kubectl logs redis-job-k7d6s

redis-job consumes item from redis FIFO queue items...
found item: apple..
redis-job done...
```

## run job-manager
```
# Gives all service accounts cluster-admin privileges (don't do it in production)
kubectl create clusterrolebinding permissive-binding --clusterrole=cluster-admin --group=system:serviceaccounts

kubectl run job-manager --image=karlkim/job-manager --command "./job-manager" --restart=Never

kubectl get pods

NAME          READY   STATUS    RESTARTS   AGE
job-manager   1/1     Running   0          40s
rdcli         2/2     Running   0          58m


kubectl exec -it rdcli -c rdcli1 -- rdcli -h redis.storage PUBLISH jobs 10

kubectl get pods

NAME                    READY   STATUS              RESTARTS   AGE
job-manager             1/1     Running             0          6m16s
rdcli                   2/2     Running             0          63m
redis-job-58d6a-cvs5t   0/1     ContainerCreating   0          0s
redis-job-58d6a-dzkfp   0/1     Completed           0          7s
redis-job-58d6a-h68cc   0/1     ContainerCreating   0          3s
redis-job-58d6a-j98br   0/1     Completed           0          7s
redis-job-58d6a-kv2jg   0/1     ContainerCreating   0          2s
redis-job-58d6a-px4lw   0/1     Completed           0          7s

kubectl logs pod/redis-job-58d6a-px4lw

redis-job-58d6a consumes item from redis FIFO queue items...
found item: banana..
redis-job done...

kubectl exec -it rdcli -c rdcli1 -- rdcli -h redis.storage keys *

1) items
2) banana
3) cherry
4) durian
5) apple

kubectl exec -it rdcli -c rdcli1 -- rdcli -h redis.storage GET cherry
4

```

## subscribe rdcli2 to jobs
```
kubectl exec -it rdcli -c rdcli2 -- rdcli -h redis.storage
redis.storage:6379> SUBSCRIBE jobs
jobs
```

### job-api
```
# create clusterrole create job
kubectl apply -f ./create-job-clusterrole.yaml

# create sa job-api-sa
kubectl create serviceaccount job-api-sa


kubectl apply -f .\job-api.yaml

# assign job-api role
kubectl create clusterrolebinding create-job-role-binding --clusterrole=create-job-role --serviceaccount=default:job-api-sa

# testing
kubectl port-forward job-api 80:80

# minikube
kubectl apply -f .\job-api-loadbalancer.yaml
minikube service job-api-loadbalancer

# ingress


```