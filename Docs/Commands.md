### ooo_add_target
- `ooo_add_target entityID`
- Order-of-Operation stepping has two types of breakpoints, normal breakpoint and for-each breakpoint. A for-each breakpoint, is a breakpoint which sits inside a for-each block.
- This command adds the entity as a for-each breakpoint in EntityList.Update()
- e.g. ooo_add_target WindController
- e.g. ooo_add_target CrystalStaticSpinner[c1:02]
- e.g. ooo_add_target DustStaticSpinner, which automatically adds all DustStaticSpinner in current room
- e.g. ooo_add_target Each, which automatically adds all entities in current room

### ooo_remove_target
- `ooo_remove_target entityID`
- Remove a for-each breakpoint in EntityList.Update()
- When entityID is not added, but "Each" is added, then it works like "Each except entityID"
- When we have a "Each except entityID", and use "ooo_add_target entityID", then we get "Each" back.
- e.g. ooo_add_target Each, ooo_remove_target Decal. Then all entities except Decal will be breakpoints.

### ooo_show_target
- `ooo_show_target`
- Show all for-each breakpoints in EntityList.Update()

### ooo_add_target_pc
- `ooo_add_target_pc entityID`
- This command adds all PlayerCollider belonging to this entity as for-each breakpoints in PlayerCollider checks
- e.g. ooo_add_target_pc Spring
- e.g. ooo_add_target_pc Spikes[a1:09]
- e.g. ooo_add_target_pc Auto
- The grammar is almost same as ooo_add_target, but without the "except" grammar
- Instead, we have "ooo_add_target_pc Auto", which makes the game automatically stop if a PlayerCollider collides with player

### ooo_remove_target_pc
- `ooo_remove_target_pc entityID`
- Remove a for-each breakpoint in PlayerCollider checks

### ooo_show_target_pc
- `ooo_show_target_pc`
- Show all for-each breakpoints in PlayerCollider checks

### ooo_add_autoskip
- `ooo_add_autoskip breakpointUID`
- e.g. ooo_add_autoskip EngineUpdate begin
- breakpointUID can contain arbitrarily many single spaces
- if you need to type consecutive spaces, type "\s" instead
- can't autoskip for-each breakpoints. Use ooo_remove_target instead

### ooo_remove_autoskip
- `ooo_remove_autoskip breakpointUID`

### ooo_show_autoskip
- `ooo_show_autoskip`
- Show all autoskipped normal breakpoints

### ooo_show_breakpoint
- `ooo_show_breakpoint`
- Show all normal breakpoints

### spinner_freeze
- `spinner_freeze true/false`
- Quick command to set Level.TimeActive 524288

### nearest_timeactive
- `nearest_timeactive targetTime startTime`
- Return the nearest possible timeactive of the target time, starting from startTime