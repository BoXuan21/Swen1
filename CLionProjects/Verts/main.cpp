#include <iostream>
#include <sys/types.h>
#include <sys/stat.h>
#include <unistd.h>
#include <dirent.h>
#include <vector>
#include <algorithm>
#include <string>

// Helper function: toLowerCase
// Used to convert a string to lowercase, making it easier to implement case-insensitive search
void toLowerCase(std::string &str) {
    std::transform(str.begin(), str.end(), str.begin(), ::tolower);
    // std::begin and end used as iterators to specify start and end of the string
    // ::tolower converts each character to lowercase
}

// Main function to search for files
// directory: current directory where we're looking
// filename: target filename
// recursive: boolean flag indicating if search should be recursive
// caseInsensitive: boolean flag for case-insensitive search
void findFile(const std::string &directory, const std::string &filename, bool recursive, bool caseInsensitive) {
    DIR *dir = opendir(directory.c_str());
    // opendir opens the directory and returns a pointer to DIR

    if (!dir) {
        std::cerr << "Error: Cannot open directory " << directory << std::endl;
        return;
    }

    struct dirent *entry;
    // readdir reads the next directory entry and returns a pointer to a dirent struct
    // which represents a file or a subdirectory
    while ((entry = readdir(dir)) != nullptr) {
        std::string entryName = entry->d_name;

        // Skip the current directory and parent directory entries
        if (entryName == "." || entryName == "..") {
            continue;
        }

        // Construct the full path of the entry by appending its name to the directory path
        std::string fullPath = directory + "/" + entryName;
        struct stat pathStat;
        // stat() retrieves information about the file specified by fullPath and stores it in pathStat
        stat(fullPath.c_str(), &pathStat);

        if (S_ISDIR(pathStat.st_mode) && recursive) {
            // If entry is a directory and recursive search is enabled, create a child process
            if (fork() == 0) {  // Child process
                findFile(fullPath, filename, recursive, caseInsensitive);
                exit(0);  // Exit child process after completing the recursive search
            }
            wait(NULL);  // Parent process waits for the child to finish
        }
        else if (S_ISREG(pathStat.st_mode)) {
            // If entry is a regular file
            std::string searchName = filename;
            std::string targetName = entryName;

            // Perform case-insensitive comparison if specified
            if (caseInsensitive) {
                toLowerCase(searchName);
                toLowerCase(targetName);
            }

            // Check if the file matches the filename we're searching for
            if (searchName == targetName) {
                std::cout << getpid() << ": " << filename << ": " << fullPath << std::endl;
            }
        }
    }

    closedir(dir);  // Close the directory after processing all entries
}

int main(int argc, char *argv[]) {
    bool recursive = false;
    bool caseInsensitive = false;
    std::string searchPath;
    std::vector<std::string> filenames;

    //to-Do Parsing Argument
    //for each filename, fork a process and search
    //and need to wait for the chuld process to finish

    return 0;
}
