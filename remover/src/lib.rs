pub mod lib {
    use std::path::{Path, PathBuf};
    use std::process::{Command, Stdio};

    pub fn has_readonly_permissions_recursive(path: &Path) -> bool {
        // Return early if the path is not a directory
        if !path.is_dir() {
            return false;
        }

        // Iterate over the entries in the directory and check their permissions
        for entry in std::fs::read_dir(path).unwrap() {
            let entry = entry.unwrap();
            let metadata = std::fs::metadata(entry.path()).unwrap();
            if metadata.permissions().readonly() {
                return true;
            }

            // If the entry is a directory, check its contents recursively
            if metadata.is_dir() {
                if has_readonly_permissions_recursive(&entry.path()) {
                    return true;
                }
            }
        }

        // Return false if no files or folders have read-only permissions
        false
    }

    pub fn get_third_level_folder(path: &Path) -> Option<&Path> {
        // Get the parent of the current folder
        let mut current_folder = path.parent()?;

        // Iterate over the next two levels of hierarchy
        for _ in 0..2 {
            current_folder = current_folder.parent()?;
        }

        Some(current_folder)
    }

    pub fn git_rm(solution_dir: &Path, violet_dir: PathBuf) -> Result<(), String> {
        let git_rm_child = Command::new("git")
            .arg("rm")
            .arg("-r")
            .arg(violet_dir)
            .stdout(Stdio::null())
            .stderr(Stdio::null())
            .current_dir(solution_dir)
            .spawn();

        if git_rm_child.is_err() {
            return Err("Failed to execute 'git rm' process".to_string());
        }

        let waited_git_rm = git_rm_child.unwrap().wait_with_output();

        if waited_git_rm.is_err() {
            return Err("Failed to wait on 'git rm' process".to_string());
        }

        if waited_git_rm.unwrap().status.success() == false {
            return Err("Git rm command was unsuccessful".to_string());
        }

        Ok(())
    }

    pub fn git_commit(solution_dir: &Path, guids: Vec<String>) -> Result<(), String> {
        let git_commit_child = Command::new("git")
            .arg("commit")
            .arg("-m")
            .arg(format!("Delete violets '{}'", guids.join(", ")))
            .stdout(Stdio::null())
            .stderr(Stdio::null())
            .current_dir(solution_dir)
            .spawn();

        if git_commit_child.is_err() {
            return Err("Failed to execute 'git commit' process".to_string());
        }

        let waited_git_commit = git_commit_child.unwrap().wait_with_output();

        if waited_git_commit.is_err() {
            return Err("Failed to wait on 'git commit' process".to_string());
        }

        if waited_git_commit.unwrap().status.success() == false {
            return Err("Git commit command was unsuccessful".to_string());
        }

        Ok(())
    }

    pub fn git_push(solution_dir: &Path) -> Result<(), String> {
        let git_push_child = Command::new("git")
            .arg("push")
            .stdout(Stdio::null())
            .current_dir(solution_dir)
            .spawn();

        if git_push_child.is_err() {
            return Err("Failed to execute 'git push' process".to_string());
        }

        let waited_git_push = git_push_child.unwrap().wait_with_output();

        if waited_git_push.is_err() {
            return Err("Failed to wait on 'git push' process".to_string());
        }

        if waited_git_push.unwrap().status.success() == false {
            return Err("Git push command was unsuccessful".to_string());
        }

        Ok(())
    }
}

#[cfg(test)]
mod tests {
    use crate::lib::get_third_level_folder;
    use std::path::Path;

    #[test]
    fn test_get_third_level_folder() {
        // Test getting the third level folder from a path with three levels of hierarchy
        let path = Path::new("/a/b/c/d");
        let third_level_folder = get_third_level_folder(&path);
        assert_eq!(third_level_folder, Some(Path::new("/a")));

        // Test getting the third level folder from a path with two levels of hierarchy
        let path = Path::new("/a/b/c");
        let third_level_folder = get_third_level_folder(&path);
        assert_eq!(third_level_folder, Some(Path::new("/")));

        // Test getting the third level folder from a path with one level of hierarchy
        let path = Path::new("/a/b");
        let third_level_folder = get_third_level_folder(&path);
        assert_eq!(third_level_folder, None);

        // Test getting the third level folder from a path with no hierarchy
        let path = Path::new("/a");
        let third_level_folder = get_third_level_folder(&path);
        assert_eq!(third_level_folder, None);
    }
}
