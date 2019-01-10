.PHONY: test
FILE=pr2jiraTransitions.cs

run:
	func host start
	
newapp:
	func init func-test
	func new --name MyHttpTrigger --template "HttpTrigger"
	#func host start --build

pretty:
	uncrustify -c /linux.cfg  -f ${FILE} > /tmp/tmp
	mv /tmp/tmp ${FILE} 

test:
	./test_jira_app.sh 
