FUNCTION_NAME=c72a634c-7f41-c40f-1094-87a090d84b5d

echo first test bad branch name
curl -d "$(cat HD246-test.json | sed 's/HD-246/BROKEN/g')" -v  http://localhost:7071/api/${FUNCTION_NAME}  -H "Content-Type: application/json" 

sleep 3
echo "second test DevOps debug test 'mytopic'"
curl -d "$(cat HD246-test.json | sed 's/HD-246/mytopic/g')" -v  http://localhost:7071/api/${FUNCTION_NAME}  -H "Content-Type: application/json" 


sleep 3
echo  final test change transition of ticket
curl -d "$(cat HD246-test.json | sed 's/HD-246/HD-246/g')" -v  http://localhost:7071/api/${FUNCTION_NAME}  -H "Content-Type: application/json" 


exit
 
