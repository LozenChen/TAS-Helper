### ooo_add_target
- `ooo_add_target entityID`
- Order-of-Operation stepping has two types of breakpoints, normal breakpoint and for-each breakpoint. A for-each breakpoint, is a breakpoint which sits inside a for-each block.
- This command add the entity as a for-each breakpoint of the OoO stepping
- e.g. ooo_add_target WindController
- e.g. ooo_add_target CrystalStaticSpinner[c1:02]
- e.g. ooo_add_target DustStaticSpinner[%], which automatically replaces "%" with all entityIDs of DustStaticSpinner in current room
- Use "\s" when typing space

### ooo_remove_target

### ooo_show_target

### ooo_add_autoskip
- `ooo_add_autoskip breakpointUID`
- e.g. ooo_add_autoskip EngineUpdate\sbegin
- Use "\s" when typing space

### ooo_remove_autoskip

### ooo_show_autoskip

### ooo_show_breakpoint

### spinner_freeze
- `spinner_freeze true/false`
- Quick command to set Level.TimeActive 524288

### nearest_timeactive
- `nearest_timeactive targetTime startTime`
- Return the nearest possible timeactive of the target time, starting from startTime