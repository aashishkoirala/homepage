@echo off
rem Arguments expected:
rem  <kubernetes-cluster> <resource-group> <namespace> <acr-name> <sp-id> <sp-pwd>
cd %~dp0
helm repo update
call az aks get-credentials -n %1 -g %2 --overwrite-existing
kubectl config use-context %1
kubectl create namespace %3
kubectl config set-context --current --namespace=%3
kubectl apply -f tiller-rbac.yaml
kubectl apply -f secrets.yaml
helm init --service-account tiller --tiller-namespace %3
helm install stable/nginx-ingress -n ak-nginx-ingress-controller --namespace %3 --set controller.replicaCount=1 --set controller.nodeSelector."beta\.kubernetes\.io/os"=linux --set defaultBackend.nodeSelector."beta\.kubernetes\.io/os"=linux --tiller-namespace %3
bash -c "openssl req -x509 -nodes -out cert.crt -keyout key.key -subj '/C=US/ST=MA/L=Boston/O=Aashish Koirala/CN=k8s.aashishkoirala.com'"
kubectl create secret tls ak-prod-tls --cert=cert.crt --key=key.key
del cert.crt
del key.key
kubectl apply -f ingress.yaml
kubectl create secret docker-registry ak-prod-image-pull-secret --docker-server=https://%4.azurecr.io --docker-username=%5 --docker-password=%6