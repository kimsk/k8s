# docker
```
dotnet publish -c Release -f netcoreapp3.1 -r linux-x64 --self-contained true

docker build -t karlkim/job-manager .

docker push karlkim/job-manager
```

# test
```
kubectl run job-manager --image=karlkim/job-manager --command "./job-manager" --restart=Never
```