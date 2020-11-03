# docker
```
dotnet publish -c Release -f netcoreapp3.1 -r linux-x64 --self-contained true

docker build -t karlkim/redis-job .

docker push karlkim/redis-job
```