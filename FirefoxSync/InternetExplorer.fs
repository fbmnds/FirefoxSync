namespace FirefoxSync

open System.Collections.Generic
open System.IO

open Results
open Utilities
open Collections

module InternetExplorer =
    
    let IExplorerPasswordStore = @"%USERPROFILE%\AppData\Local\Microsoft\Credentials"

    let private str'  (WeaveGUID x) = x
    let private str'' (URI x)       = x

    let bookmarkToUrl (bm: Bookmarks) =
        if bm.``type`` = "folder" then None 
        else
            ["[DEFAULT]\n"          ;
             "BASEURL="             ; (str'' bm.bmkUri)                ; "\n" ;
             "[InternetShortcut]\n" ; 
             "URL="                 ; (str'' bm.bmkUri)                ; "\n" ;
             "IDList="              ; bm.keyword                       ; "\n" ;
             "IconFile=\n"          ;
             "IconIndex=\n"         ;
             "[Firefox]\n"          ;
             "Description="         ; bm.description                   ; "\n" ;
             "Tags="                ; (bm.tags |> (String.concat ",")) ; "\n" ]
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


    let buildFolderPaths (folders: Bookmarks list) =
        let dictOfPaths = new Dictionary<WeaveGUID, string list>(HashIdentity.Structural)
        if folders.IsEmpty then Success dictOfPaths
        else
            let dictOfParents = new Dictionary<WeaveGUID, WeaveGUID option>(HashIdentity.Structural)
            let dictOfNames =   new Dictionary<WeaveGUID, string>(HashIdentity.Structural)
            let isTop (id : WeaveGUID) = (str' id) = "toolbar"
            let isTopLevel (id : WeaveGUID) = 
                [ "places" ; "menu" ; "unfiled" ; "unknown" ] 
                |> List.map (WeaveGUID) 
                |> Set.ofList 
                |> Set.contains id
            let folders' =
                folders
                |> List.filter 
                    (fun bm -> 
                        if isTop bm.id 
                        then
                            //dictOfNames.[bm.id] <- root
                            dictOfParents.[bm.id] <- None
                            dictOfPaths.[bm.id] <- []      // accept overwrites
                            false
                        elif isTopLevel bm.id
                        then
                            dictOfNames.[bm.id] <- (str' bm.id)
                            dictOfParents.[bm.id] <- Some ((WeaveGUID) "toolbar")
                            dictOfPaths.[bm.id] <- [ (str' bm.id) ]      // accept overwrites
                            false
                        else 
                            dictOfNames.[bm.id] <- bm.title
                            dictOfParents.[bm.id] <- Some bm.parentid // accept overwrites
                            true)
                |> List.map (fun bm -> if isTop bm.id
                                       then (bm, None,                         [],       Set.ofList [])
                                       elif isTopLevel bm.id
                                       then (bm, Some ((WeaveGUID) "toolbar"), [str' bm.id], Set.ofList [])
                                       else (bm, Some bm.parentid,             [bm.title],   Set.ofList []))
            if folders'.IsEmpty then Success dictOfPaths
            else
                let checkCurrentFolder ((bm: Bookmarks),
                                        (parentId: WeaveGUID option),
                                        (path: string list), 
                                        (ancestors: Set<WeaveGUID>)) =
                    match parentId with 
                    | None -> (bm, None, [], ancestors.Add ((WeaveGUID) "toolbar"))
                              |> Some
                              |> Success
                    | Some parentId ->
                        if ancestors.Contains parentId then 
                            [ (CyclicBookmarkFolders 
                                (((ErrorLabel) "Cyclic bookmark folder detected."),
                                 ((Stacktrace) (sprintf "Cycle detected at: '%s',\n parentid '%s',\n ancestors '%A',\n path '%s'." 
                                                    bm.title 
                                                    (str' bm.parentid)
                                                    ancestors
                                                    (path |> String.concat ","))))) ]
                            |> Failure
                        elif dictOfPaths.ContainsKey(parentId) then  
                            dictOfPaths.[bm.id] <- List.append dictOfPaths.[parentId] path
                            Success None
                        else    
                            try 
                                (bm, 
                                 dictOfParents.[parentId], 
                                 dictOfNames.[parentId] :: path, 
                                 ancestors.Add parentId)
                                |> Some
                                |> Success 
                            with
                            | exn -> (bm, 
                                      dictOfParents.[((WeaveGUID) "unknown")], 
                                      dictOfNames.[((WeaveGUID) "unknown")] :: path, 
                                      ancestors.Add ((WeaveGUID) "unknown"))
                                      |> Some
                                      |> Success
                let rec iterateOnFolder ((bm: Bookmarks),
                                          (parentId: WeaveGUID option),
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


    let folderPathToJsonString (folderPath : Dictionary<WeaveGUID, string list>) =
        (folderPath.Keys, folderPath.Values)
        |> fun (x,y) -> Seq.zip x y
        |> Seq.map 
            (fun (x,y) ->
                 let y' = sprintf "\"%s\"" (y |> List.map escapeString |> String.concat "\", \"")
                 if y'.Length > 2 
                 then sprintf "\"%s\" : [%s]" (str' x) y'
                 else sprintf "\"%s\" : []"   (str' x) )
        |> String.concat ",\n"
        |> sprintf "{ \"folderPaths\" : [\n%s\n] }"


    let createFolderTree root (dictOfFolders : Dictionary<WeaveGUID, string list>) =
        let separator  = Path.DirectorySeparatorChar.ToString()
        maybe {
            let! path = getIExplorerFavoritesFolder ()
            let! unknownFolder = sprintf "%s%s%s" path separator "unknown" |> tryCreateDirectory
            return 
                dictOfFolders
                |> fun x -> Seq.zip x.Keys x.Values
                |> Array.ofSeq
                |> Array.sortWith (fun (x,y) (x',y') -> if   y.Length < y'.Length then 1
                                                        elif y.Length = y'.Length then 0
                                                        else                          -1)
                |> Array.Parallel.map (fun (x,y) -> (x,(y |> String.concat separator)))
                |> Array.Parallel.map (fun (x,y) -> (x,sprintf "%s%s%s%s%s" path separator root separator y))
                |> Array.map (fun (x,y) -> (x,y |> tryCreateDirectory))
                |> Array.filter (fun (x,y) -> match y with | Success y -> true | _ -> false)
                |> (dict) 
        }

 
    let importBookmarks secrets cryptokeys root =
        let separator  = Path.DirectorySeparatorChar
        maybe {
            let! favoritesFolder = (getIExplorerFavoritesFolder())
            let unknownFolder = (sprintf "%s%c%s%c" favoritesFolder separator "unknown" separator)
            let! bms = getBookmarks secrets cryptokeys
            let (folders,links) = getFoldersAndLinks bms
            let! dictOfFolders' = buildFolderPaths folders
            let! dictOfFolders = createFolderTree root dictOfFolders'
            let (links',linksFailedToUrlTextConversion) = 
                links 
                |> List.map (fun x -> (x, (bookmarkToUrl x)))
                |> List.fold  
                    (fun (hasUrl,failed) (bm,text) -> 
                        if text.IsSome 
                        then
                            (((bm.id, bm.parentid, bm.title, text.Value) :: hasUrl), failed)
                        else 
                            (hasUrl, (bm.id :: failed))) 
                    ([],[]) 
                
            let (links'',linksFailedFolderLookup) = 
                links'
                |> List.fold
                    (fun (toPathOk,failed) (id,folder,title,text) -> 
                        match (dictOfFolders.TryGetValue folder) with
                        | (true, Success x) -> 
                            ((id, (sprintf "%s%c%s.url" x separator (cleanFilename title)), text) :: toPathOk, failed)
                        | _ -> 
                            ((id, (sprintf "%s%s.url" unknownFolder (cleanFilename title)), text) :: toPathOk, (id :: failed)))
                    ([],[]) 

            let (links''',linksFailedWriteToDisk) =
                links''
                |> List.fold 
                    (fun (toFileOk,failed) (id, path, text) -> 
                        match (writeStringToFile false path text) with 
                        | Success x -> ((id :: toFileOk),failed)
                        | Failure x -> (toFileOk, (id :: failed)))
                    ([],[])

            return (links, linksFailedToUrlTextConversion, linksFailedFolderLookup, linksFailedWriteToDisk)
        }

