Repositório gerado para manter o código fonte da azure function do projeto tech challenge fase 2.

Azure function está publicada no Azure.

Para executar basta abrir o Postman, executar um POST na url:

https://az-orderapproval.azurewebsites.net/api/HttpStart_OrderApproval/

Exemplo de body:

{
    "ServiceId": "1",
    "VehicleName": "",
    "ServicePrice": 225.99,
    "paid": true
}
