namespace FirefoxSync

open System.Collections.Generic


module InternetExplorer =

    let private str'  (WeaveGUID x) = x
    let private str'' (URI x)       = x

    let bookmarkToUrl (bm: Bookmarks) =
        if bm.``type`` = "folder" then None 
        else
            ["[DEFAULT]\n"  ;
             "BASEURL="     ; (str'' bm.bmkUri) ; "\n" ;
             "[InternetShortcut]\n" ; 
             "URL="         ; (str'' bm.bmkUri) ; "\n" ;
             "IDList="      ; bm.keyword        ; "\n" ;
             "IconFile=\n"  ;
             "IconIndex=\n" ;
             "[Firefox]\n"  ;
             "Description=" ; bm.description    ; "\n" ;
             "Tags="        ; (bm.tags |> (String.concat ",")) ; "\n"]
             |> (String.concat "")
             |> Some

    
    let getFoldersAndLinks (bms: Bookmarks[]) =
        bms
        |> Array.fold  
            (fun (folder,links) bm -> 
                if bm.``type`` = "folder" 
                then
                    ((bm :: folder), links)
                else 
                    (folder, (bm :: links))) ([],[]) 


    let buildFolderPaths (root: string) (folders: Bookmarks list) =
        let dictOfPaths = new Dictionary<WeaveGUID, string list>(HashIdentity.Structural)
        if folders.IsEmpty then Success dictOfPaths
        else
            let dictOfParents = new Dictionary<WeaveGUID, WeaveGUID>(HashIdentity.Structural)
            let dictOfNames =   new Dictionary<WeaveGUID, string>(HashIdentity.Structural)
            let folders' =
                folders
                |> List.filter 
                    (fun bm -> dictOfNames.[bm.id] <- str'' bm.bmkUri
                               if bm.parentid = ((WeaveGUID) "") 
                               then
                                   dictOfPaths.[bm.id] <- [ root ]      // accept overwrites
                                   false
                               else 
                                   dictOfParents.[bm.id] <- bm.parentid // accept overwrites
                                   true)
                |> List.map (fun bm -> (bm, bm.parentid, [(str'' bm.bmkUri)], Set.ofList [bm.parentid]))
            if folders'.IsEmpty then Success dictOfPaths
            else
                let checkCurrentFolder ((bm: Bookmarks),
                                        (parentId: WeaveGUID),
                                        (path: string list), 
                                        (ancestors: Set<WeaveGUID>)) =
                    if ancestors.Contains parentId then 
                        [ (CyclicBookmarkFolders 
                            (((ErrorLabel) "Cyclic bookmark folder detected."),
                             ((Stacktrace) (sprintf "Cycle detected at: '%s'." (str'' bm.bmkUri))))) ]
                        |> Failure
                    elif dictOfPaths.ContainsKey(parentId) then  
                        dictOfPaths.[bm.id] <- List.append dictOfPaths.[parentId] path
                        Success None
                    else    
                        (bm, dictOfParents.[parentId], dictOfNames.[parentId] :: path, ancestors.Add parentId)
                        |> Some
                        |> Success 
                let rec iterateOnFolder ((bm: Bookmarks),
                                          (parentId: WeaveGUID),
                                          (path: string list), 
                                          (ancestors: Set<WeaveGUID>)) =
                    let checkResult =
                        (bm, parentId, path, ancestors)
                        |> checkCurrentFolder
                    match checkResult with
                    | Failure checkResult -> Failure checkResult
                    | Success None -> Success true
                    | Success (Some x) -> iterateOnFolder x
                folders' 
                |> List.map iterateOnFolder
                |> List.fold (fun (x,z) y -> 
                                 match y with 
                                 | Failure y -> ((x && false),
                                                 [ (CyclicBookmarkFolders 
                                                       (((ErrorLabel) "Cyclic bookmark folder detected."),
                                                        ((Stacktrace) "Iteration on folder stopped."))) ] @ y) 
                                 | _ -> ((x && true), z)) 
                             (true,[])
                |> fun (x,y) -> if x then Success dictOfPaths 
                                else [ (CyclicBookmarkFolders 
                                           (((ErrorLabel) "Cyclic bookmark folder(s) detected."),
                                            ((Stacktrace) "Build folder path dictionary failed."))) ] @ y
                                     |> Failure

