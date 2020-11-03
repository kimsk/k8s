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

# redis

## create storage namespace
```
kubectl apply -f ./storage-ns.yaml

kubectl get ns

NAME                 STATUS   AGE
default              Active   9m28s
kube-node-lease      Active   9m30s
kube-public          Active   9m30s
kube-system          Active   9m30s
local-path-storage   Active   9m16s
storage              Active   12s
```

## create redis-master pod
```
kubectl apply -f ./redis-pod.yaml

kubectl get pods --namespace storage

NAME           READY   STATUS    RESTARTS   AGE
redis-master   1/1     Running   0          24s
```

### testing with rdcli (npm install -g redis-cli)
```
kubectl port-forward --namespace storage redis-master 6379:6379

rdcli ping
PONG
```

## create redis service
```
kubectl apply -f ./redis-service.yaml

kubectl get svc --namespace storage

NAME    TYPE        CLUSTER-IP     EXTERNAL-IP   PORT(S)    AGE
redis   ClusterIP   10.96.183.64   <none>        6379/TCP   11s
```

## create rdcli pod
```
kubectl apply -f ./rdcli-pod.yaml

kubectl get pods

NAME    READY   STATUS    RESTARTS   AGE
rdcli   1/1     Running   0          3m28s

kubectl exec -it rdcli -- rdcli -h redis.storage ping

PONG

kubectl exec -it rdcli -- rdcli -h redis.storage rpush jobs "apple" "banana" "cherry" "durian"
(integer) 4

kubectl exec -it rdcli -- rdcli -h redis.storage keys *

1) jobs

kubectl exec -it rdcli -- rdcli -h redis.storage lrange jobs 0 -1

1) apple
2) banana
3) cherry
4) durian
```

## create cronjob to count # of jobs
```
kubectl apply -f ./count-jobs-cronjob.yaml

NAME         SCHEDULE      SUSPEND   ACTIVE   LAST SCHEDULE   AGE
count-jobs   */1 * * * *   False     0        25s             6m45s

kubectl get cronjob count-jobs

NAME         SCHEDULE      SUSPEND   ACTIVE   LAST SCHEDULE   AGE
count-jobs   */1 * * * *   False     1        11s             12s

kubectl get pods

NAME                          READY   STATUS      RESTARTS   AGE
count-jobs-1604427540-xltnm   0/1     Completed   0          20s
rdcli                         1/1     Running     0          36m

kubectl logs count-jobs-1604427540-xltnm

(integer) 4
```

## test F# redis-job
```
kubectl run -i --tty --rm redis-job --image karlkim/redis-job --command "./redis-job"

If you don't see a command prompt, try pressing enter.
no item left..
consume item: apple..
consume item: banana..
consume item: cherry..
consume item: durian..
no item left..
no item left..
```

## create F# redis-job
```
kubectl apply -f ./redis-job.yaml

kubectl logs --follow <redis-job pod-name>

consume item from redis FIFO queue...
no item left..
no item left..
no item left..
no item left..
no item left..
consume item: cherry..
no item left..
no item left..
```

