.PHONY: format

format:
	find . -regex '.*\.\(cs\)' -exec clang-format -style=file -i {} \;
