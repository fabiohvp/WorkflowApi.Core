# WorkflowApi.Core

### NOTE: This project is an experiment.

This framework is made to reduce your api endpoints by having them created by behavior. It has a class Processor that expects an array of Request (custom class) and process them.  
It also enforces the use of workflow pattern and should help you keep your project organized.

I suggest looking at these links to have a better understanding:
1. How to setup your endpoints and some workflows examples
    1. https://github.com/fabiohvp/ControleSocial/blob/master/Server/src/Controllers/DWControleSocialController.cs
    2. https://github.com/fabiohvp/ControleSocial/tree/master/Server/src/Workflows/Municipio/Despesa
2. How make a request and how to write your workflows calls
    1. https://github.com/fabiohvp/ControleSocial/blob/master/Client/src/stores/municipio/despesa.js each function return an array that can call multiple workflows that will be chained
    2. https://github.com/fabiohvp/ControleSocial/blob/master/Client/src/pages/municipio/%5Bano%5D/%5Bmunicipio%5D/%5Bug%5D/despesas/index.svelte line:29 pass multiple arrays of workflows, each one will be executed asynchronously but they should all return together since it is one request (**note**: in this example I used websocket so they will return individually)

Pros:
1. Have few endpoints (example: one for general requests, one for upload, and others should you need different behaviors)
2. You can call multiple workflows (by default they are async) (you can also chain their results) with only one request to your backend
3. Workflows (that are not chained) will be executed async by default

Cons:
1. It uses ActivatorUtilities.CreateInstance to instantiate workflows and you need to write your custom logic to map your workflow name from Request to the Workflow class.
2. You have to be careful with which workflow you expose to your client side (you can handle this when you are write your logic to map workflows).
3. Arguments to workflows constructors are passed as JSON object so for numbers you may need to create a constructor that receive long and cast to your actual type.

PS: Workflows must implement IWorkflow or inherit Workflow/QueryableWorkflow
