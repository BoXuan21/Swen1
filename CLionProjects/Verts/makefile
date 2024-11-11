# Define the target name (name of the executable)
TARGET = myfind

# Specify the compiler and standard options
CXX = g++
CXXFLAGS = -std=c++14 -Wall -Werror

# Build all: specifies that `all` will build the main target
all: $(TARGET)

# Rule to build the `myfind` executable
$(TARGET): myfind.o
	$(CXX) $(CXXFLAGS) -o $(TARGET) myfind.o

# Rule to compile myfind.cpp into an object file
myfind.o: myfind.cpp
	$(CXX) $(CXXFLAGS) -c myfind.cpp -o myfind.o

# Clean rule to remove the executable and object files
clean:
	rm -f $(TARGET) myfind.o
