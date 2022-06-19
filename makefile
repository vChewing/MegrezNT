.PHONY: format pack

format:
	find . -regex '.*\.\(cs\)' -exec clang-format -style=file -i {} \;
pack:
	dotnet build --configuration Release --no-restore ; dotnet test ; dotnet pack --configuration Release
