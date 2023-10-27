### ooo_add_target
- `ooo_add_target entityID`
- Add the entity as a for-each breakpoint of the OoO stepping
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