﻿namespace FirefoxSync

// Firefox Sync Server URLs

module ServerUrls =
    open Utilities

    let serverURL username = "https://auth.services.mozilla.com/user/1.0/" + username + "/node/weave"
    let clusterURL username = username |> serverURL |> fetchUrlResponse "GET" None None None None 

 