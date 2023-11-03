### ooo_add_target
- `ooo_add_target entityID`
- Order-of-Operation stepping has two types of breakpoints, normal breakpoint and for-each breakpoint. A for-each breakpoint, is a breakpoint which sits inside a for-each block.
- This command add the entity as a for-each breakpoint of the OoO stepping
- e.g. ooo_add_target WindController
- e.g. ooo_add_target CrystalStaticSpinner[c1:02]
- e.g. ooo_add_target DustStaticSpinner[%], which automatically replaces "%" with entityIDs of all DustStaticSpinner in current room
- e.g. ooo_add_target Each[%], which automatically replaces this with entityIDs of all entities in current room

### ooo_remove_target
- `ooo_remove_target entityID`
- Remove a for-each breakpoint of the OoO stepping

### ooo_show_target
- `ooo_show_target`
- Show all for-each breakpoints of the OoO stepping

### ooo_add_autoskip
- `ooo_add_autoskip breakpointUID`
- e.g. ooo_add_autoskip EngineUpdate\sbegin
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