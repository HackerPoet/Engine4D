#define PROC_VERT
#define apply_proc_vert4D_init()
#define apply_proc_vert4D(v) \
        v -= _CamPosition;
#define apply_proc_vert5D_init()
#define apply_proc_vert5D(v, V) \
        v -= _CamPosition; V -= _CamPosition_V;
